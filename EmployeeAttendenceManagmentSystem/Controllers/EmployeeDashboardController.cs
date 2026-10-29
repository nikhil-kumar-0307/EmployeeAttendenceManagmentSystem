using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using EmployeeAttendenceManagmentSystem.Models;
using EmployeeAttendenceManagmentSystem.Models.Punches;
using EmployeeAttendenceManagmentSystem.Models.Qms;
using EmployeeAttendenceManagmentSystem.Models.ViewModels;

namespace EmployeeAttendenceManagmentSystem.Controllers
{
    public class EmployeeDashboardController : Controller
    {
        private readonly AttendanceDbContext _db = new AttendanceDbContext();   // reporting lines, admins
        private readonly QmsDbContext _qms = new QmsDbContext();                // employee master (names, department)
        private readonly PunchDbContext _log = new PunchDbContext();            // attendance punches

        private string Me { get { return Session["EmployeeCode"] as string; } }

        // Only a signed-in Employee may use anything in this controller
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Me == null || (Session["Role"] as string) != UserRoles.Employee)
            {
                FormsAuthentication.SignOut();
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: /EmployeeDashboard
        public ActionResult Index()
        {
            var me = Me;
            var vm = new EmployeeHomeVm
            {
                DisplayName = FirstName(Session["FullName"] as string),
                SubordinateCount = _db.ReportingLines.Count(r => r.ManagerEmployeeNo == me)
            };
            return View(vm);
        }

        // GET: /EmployeeDashboard/MyAttendance?month=2026-10
        public ActionResult MyAttendance(string month)
        {
            ViewBag.Crumb = "My attendance";
            return View("MonthlyAttendance", BuildMonth(Me, ParseMonth(month), true));
        }

        // GET: /EmployeeDashboard/SubordinateAttendance?date=2026-10-29
        public ActionResult SubordinateAttendance(string date)
        {
            ViewBag.Crumb = "Subordinate attendance";
            var me = Me;
            var day = ParseDate(date);

            var numbers = _db.ReportingLines
                .Where(r => r.ManagerEmployeeNo == me)
                .Select(r => r.EmployeeNo).ToList();

            // punches of that day, summarised per employee
            var summaries = Summaries(numbers, day, day.AddDays(1))
                .ToDictionary(s => s.EmpCode, StringComparer.OrdinalIgnoreCase);

            // names and designations from QMS
            var qmsMap = _qms.EmployeeMasters
                .Where(e => numbers.Contains(e.EmployeeNo)).ToList()
                .GroupBy(e => e.EmployeeNo)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // anyone missing from QMS: fall back to the name stored in the punch log
            var logNames = NamesFromLog(numbers.Where(n => !qmsMap.ContainsKey(n)).ToList());

            var vm = new SubordinateDayVm { Date = day };
            foreach (var no in numbers)
            {
                QmsEmployee emp; PunchSummary s; string logName;
                qmsMap.TryGetValue(no, out emp);
                summaries.TryGetValue(no, out s);
                logNames.TryGetValue(no, out logName);

                var row = new SubordinateRow
                {
                    EmployeeNo = no,
                    Name = emp != null ? TitleCase(emp.EmployeeName) : TitleCase(logName ?? no),
                    Designation = emp != null ? emp.Designation : "",
                    Status = StatusFor(day, s != null),
                    InText = s != null ? s.First.ToString("HH:mm") : "-",
                    OutText = s != null && s.Count > 1 ? s.Last.ToString("HH:mm") : "-"
                };
                vm.Rows.Add(row);

                switch (row.Status)
                {
                    case AttendanceStatus.Present: vm.Present++; break;
                    case AttendanceStatus.Absent: vm.Absent++; break;
                    case AttendanceStatus.WeeklyOff: vm.WeeklyOff++; break;
                    default: vm.NotMarked++; break;
                }
            }
            vm.Rows = vm.Rows.OrderBy(r => r.Name).ToList();
            return View(vm);
        }

        // GET: /EmployeeDashboard/SubordinateMonthly?employeeNo=9001&month=2026-10
        public ActionResult SubordinateMonthly(string employeeNo, string month)
        {
            var me = Me;
            // A manager may only open people who report to them
            bool allowed = !string.IsNullOrEmpty(employeeNo) &&
                _db.ReportingLines.Any(r => r.ManagerEmployeeNo == me && r.EmployeeNo == employeeNo);
            if (!allowed)
                return new HttpStatusCodeResult(403, "This employee does not report to you.");

            ViewBag.Crumb = "Subordinate attendance";
            return View("MonthlyAttendance", BuildMonth(employeeNo, ParseMonth(month), false));
        }

        // ---------------- attendance logic ----------------

        // Present  = at least one punch that day
        // Weekly off = no punch on a Sunday
        // Not marked = no punch yet today / in the future
        // Absent   = no punch on a past working day
        private static string StatusFor(DateTime date, bool hasPunch)
        {
            if (hasPunch) return AttendanceStatus.Present;
            if (date.DayOfWeek == DayOfWeek.Sunday) return AttendanceStatus.WeeklyOff;
            if (date.Date >= DateTime.Today) return AttendanceStatus.NotMarked;
            return AttendanceStatus.Absent;
        }

        // First punch = In, last punch = Out (when there is more than one punch)
        private List<PunchSummary> Summaries(List<string> codes, DateTime start, DateTime end)
        {
            if (codes.Count == 0) return new List<PunchSummary>();

            return _log.Punches
                .Where(p => codes.Contains(p.EmpCode)
                            && p.TransactionDateTime >= start
                            && p.TransactionDateTime < end)
                .GroupBy(p => new { p.EmpCode, Day = DbFunctions.TruncateTime(p.TransactionDateTime) })
                .Select(g => new PunchSummary
                {
                    EmpCode = g.Key.EmpCode,
                    Day = g.Key.Day,
                    First = g.Min(x => x.TransactionDateTime),
                    Last = g.Max(x => x.TransactionDateTime),
                    Count = g.Count(),
                    Name = g.Max(x => x.EmplName)
                })
                .ToList();
        }

        private Dictionary<string, string> NamesFromLog(List<string> codes)
        {
            if (codes.Count == 0) return new Dictionary<string, string>();

            return _log.Punches
                .Where(p => codes.Contains(p.EmpCode))
                .GroupBy(p => p.EmpCode)
                .Select(g => new { Code = g.Key, Name = g.Max(x => x.EmplName) })
                .ToList()
                .ToDictionary(x => x.Code, x => x.Name, StringComparer.OrdinalIgnoreCase);
        }

        private MonthlyAttendanceVm BuildMonth(string employeeNo, DateTime month, bool isSelf)
        {
            var emp = _qms.EmployeeMasters.FirstOrDefault(e => e.EmployeeNo == employeeNo);
            var end = month.AddMonths(1);

            var perDay = Summaries(new List<string> { employeeNo }, month, end)
                .Where(s => s.Day.HasValue)
                .ToDictionary(s => s.Day.Value.Date);

            string name = emp != null ? emp.EmployeeName : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                string logName;
                NamesFromLog(new List<string> { employeeNo }).TryGetValue(employeeNo, out logName);
                name = logName ?? employeeNo;
            }

            var vm = new MonthlyAttendanceVm
            {
                EmployeeNo = employeeNo,
                EmployeeName = TitleCase(name),
                Department = emp != null ? emp.Department : "",
                Designation = emp != null ? emp.Designation : "",
                Month = month,
                IsSelf = isSelf
            };

            int days = DateTime.DaysInMonth(month.Year, month.Month);
            for (int i = 0; i < days; i++)
            {
                var date = month.AddDays(i);
                PunchSummary s;
                perDay.TryGetValue(date, out s);

                var row = new DayRow
                {
                    Date = date,
                    Status = StatusFor(date, s != null),
                    InText = s != null ? s.First.ToString("HH:mm") : "-",
                    OutText = s != null && s.Count > 1 ? s.Last.ToString("HH:mm") : "-",
                    HoursText = s != null ? HoursText(s) : "-",
                    Punches = s != null ? s.Count : 0
                };
                vm.Rows.Add(row);

                switch (row.Status)
                {
                    case AttendanceStatus.Present: vm.Present++; break;
                    case AttendanceStatus.Absent: vm.Absent++; break;
                    case AttendanceStatus.WeeklyOff: vm.WeeklyOff++; break;
                    default: vm.NotMarked++; break;
                }
            }
            return vm;
        }

        // ---------------- small helpers ----------------
        private static string HoursText(PunchSummary s)
        {
            if (s.Count < 2 || s.Last <= s.First) return "-";
            var span = s.Last - s.First;
            return string.Format("{0}h {1:00}m", (int)span.TotalHours, span.Minutes);
        }

        private static DateTime ParseMonth(string month)
        {
            DateTime d;
            if (!string.IsNullOrEmpty(month) &&
                DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return new DateTime(d.Year, d.Month, 1);

            var now = DateTime.Today;
            return new DateTime(now.Year, now.Month, 1);
        }

        private static DateTime ParseDate(string date)
        {
            DateTime d;
            if (!string.IsNullOrEmpty(date) &&
                DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.Date;
            return DateTime.Today;
        }

        private static string TitleCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.Trim().ToLower());
        }

        private static string FirstName(string fullName)
        {
            var name = TitleCase(fullName);
            return name.Split(' ').FirstOrDefault() ?? name;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _db.Dispose(); _qms.Dispose(); _log.Dispose(); }
            base.Dispose(disposing);
        }
    }
}