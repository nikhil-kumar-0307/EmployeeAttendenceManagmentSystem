using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using EmployeeAttendenceManagmentSystem.Helpers;
using EmployeeAttendenceManagmentSystem.Models;
using EmployeeAttendenceManagmentSystem.Models.Punches;
using EmployeeAttendenceManagmentSystem.Models.Qms;
using EmployeeAttendenceManagmentSystem.Models.ViewModels;

namespace EmployeeAttendenceManagmentSystem.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly AttendanceDbContext _db = new AttendanceDbContext();   // reporting lines
        private readonly QmsDbContext _qms = new QmsDbContext();                // employees (read-only)
        private readonly PunchDbContext _log = new PunchDbContext();            // fallback names (read-only)

        // Only a signed-in Admin may use anything in this controller
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["EmployeeCode"] == null || (Session["Role"] as string) != UserRoles.Admin)
            {
                FormsAuthentication.SignOut();
                filterContext.Result = RedirectToAction("Login", "Account");
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: /AdminDashboard
        public ActionResult Index()
        {
            var vm = new AdminHomeVm
            {
                DisplayName = FirstName(Session["FullName"] as string),
                ManagerCount = _db.ReportingLines.Select(r => r.ManagerEmployeeNo).Distinct().Count(),
                AssignedCount = _db.ReportingLines.Select(r => r.EmployeeNo).Distinct().Count(),
                EmployeeCount = _qms.EmployeeMasters.Count(),
                TopManagers = GetManagerRows().Take(8).ToList()
            };
            return View(vm);
        }

        // GET: /AdminDashboard/Assignments?q=...
        // Find a manager, and see every manager that already has a team
        public ActionResult Assignments(string q)
        {
            var vm = new AssignmentsVm { Query = (q ?? "").Trim() };

            var groups = _db.ReportingLines
                .GroupBy(r => r.ManagerEmployeeNo)
                .Select(g => new { Manager = g.Key, Count = g.Count() })
                .ToList();
            var teamSize = groups.ToDictionary(g => g.Manager, g => g.Count, StringComparer.OrdinalIgnoreCase);

            var people = Lookup(groups.Select(g => g.Manager));
            vm.Managers = groups
                .Select(g => new ManagerRow
                {
                    EmployeeNo = g.Manager,
                    Name = people[g.Manager].Name,
                    Department = people[g.Manager].Department,
                    Designation = people[g.Manager].Designation,
                    TeamSize = g.Count
                })
                .OrderBy(m => m.Name).ToList();

            if (vm.Query.Length > 0)
            {
                foreach (var e in SearchQms(vm.Query))
                {
                    int size;
                    teamSize.TryGetValue(e.EmployeeNo, out size);
                    vm.Results.Add(new SearchRow
                    {
                        EmployeeNo = e.EmployeeNo,
                        Name = TitleCase(e.EmployeeName),
                        Department = e.Department,
                        Designation = e.Designation,
                        TeamSize = size
                    });
                }
            }
            return View(vm);
        }

        // GET: /AdminDashboard/Manage?managerNo=5866&q=...
        public ActionResult Manage(string managerNo, string q)
        {
            managerNo = (managerNo ?? "").Trim();
            if (managerNo.Length == 0)
                return RedirectToAction("Assignments");

            var teamNumbers = _db.ReportingLines
                .Where(r => r.ManagerEmployeeNo == managerNo)
                .Select(r => r.EmployeeNo).ToList();

            if (teamNumbers.Count == 0 && !_qms.EmployeeMasters.Any(e => e.EmployeeNo == managerNo))
                return HttpNotFound();

            var people = Lookup(teamNumbers.Concat(new[] { managerNo }));

            var vm = new ManageTeamVm
            {
                Manager = people[managerNo],
                Team = teamNumbers.Select(n => people[n]).OrderBy(p => p.Name).ToList(),
                Query = (q ?? "").Trim()
            };

            if (vm.Query.Length > 0)
            {
                var found = SearchQms(vm.Query)
                    .Where(e => !string.Equals(e.EmployeeNo, managerNo, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var codes = found.Select(f => f.EmployeeNo).ToList();
                var lines = _db.ReportingLines.Where(r => codes.Contains(r.EmployeeNo)).ToList();
                var managerNames = Lookup(lines.Select(l => l.ManagerEmployeeNo));

                foreach (var e in found)
                {
                    var mine = lines.Any(l => l.EmployeeNo == e.EmployeeNo
                        && string.Equals(l.ManagerEmployeeNo, managerNo, StringComparison.OrdinalIgnoreCase));
                    var other = lines.FirstOrDefault(l => l.EmployeeNo == e.EmployeeNo
                        && !string.Equals(l.ManagerEmployeeNo, managerNo, StringComparison.OrdinalIgnoreCase));

                    vm.Results.Add(new SearchRow
                    {
                        EmployeeNo = e.EmployeeNo,
                        Name = TitleCase(e.EmployeeName),
                        Department = e.Department,
                        Designation = e.Designation,
                        IsInThisTeam = mine,
                        CurrentManagerNo = (!mine && other != null) ? other.ManagerEmployeeNo : null,
                        CurrentManagerName = (!mine && other != null) ? managerNames[other.ManagerEmployeeNo].Name : null
                    });
                }
            }
            return View(vm);
        }

        // POST: /AdminDashboard/Add
        // Adds the employee to the manager's team. An employee has ONE manager,
        // so any earlier manager is replaced ("Move here").
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(string managerNo, string employeeNo, string q)
        {
            managerNo = (managerNo ?? "").Trim();
            employeeNo = (employeeNo ?? "").Trim();

            if (managerNo.Length == 0 || employeeNo.Length == 0)
                return RedirectToAction("Assignments");

            if (string.Equals(managerNo, employeeNo, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "An employee cannot report to themselves.";
                return RedirectToAction("Manage", new { managerNo, q });
            }

            if (!_qms.EmployeeMasters.Any(e => e.EmployeeNo == employeeNo))
            {
                TempData["Error"] = "Employee " + employeeNo + " was not found in the employee master.";
                return RedirectToAction("Manage", new { managerNo, q });
            }

            if (WouldCreateLoop(managerNo, employeeNo))
            {
                TempData["Error"] = "Not added: that employee is already above this manager in the reporting chain.";
                return RedirectToAction("Manage", new { managerNo, q });
            }

            // remove the employee from any other manager's team
            var others = _db.ReportingLines
                .Where(r => r.EmployeeNo == employeeNo && r.ManagerEmployeeNo != managerNo).ToList();
            bool moved = others.Count > 0;
            if (moved) _db.ReportingLines.RemoveRange(others);

            bool already = _db.ReportingLines.Any(r => r.ManagerEmployeeNo == managerNo && r.EmployeeNo == employeeNo);
            if (!already)
                _db.ReportingLines.Add(new ReportingLine { ManagerEmployeeNo = managerNo, EmployeeNo = employeeNo });

            _db.SaveChanges();

            var names = Lookup(new[] { managerNo, employeeNo });
            TempData["Success"] = already
                ? names[employeeNo].Name + " is already in this team."
                : (moved ? names[employeeNo].Name + " moved to the team of " + names[managerNo].Name + "."
                         : names[employeeNo].Name + " added to the team of " + names[managerNo].Name + ".");

            return RedirectToAction("Manage", new { managerNo, q });
        }

        // POST: /AdminDashboard/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(string managerNo, string employeeNo, string q)
        {
            managerNo = (managerNo ?? "").Trim();
            employeeNo = (employeeNo ?? "").Trim();

            var lines = _db.ReportingLines
                .Where(r => r.ManagerEmployeeNo == managerNo && r.EmployeeNo == employeeNo).ToList();

            if (lines.Count > 0)
            {
                _db.ReportingLines.RemoveRange(lines);
                _db.SaveChanges();
                TempData["Success"] = Lookup(new[] { employeeNo })[employeeNo].Name + " removed from the team.";
            }
            return RedirectToAction("Manage", new { managerNo, q });
        }

        // GET: /AdminDashboard/ResetPasswords?q=...
        public ActionResult ResetPasswords(string q)
        {
            var vm = new ResetPasswordsVm { Query = (q ?? "").Trim() };

            if (vm.Query.Length > 0)
            {
                vm.Results = SearchQms(vm.Query).Select(e => new ResetRow
                {
                    EmployeeNo = e.EmployeeNo,
                    Name = TitleCase(e.EmployeeName),
                    Department = e.Department,
                    Designation = e.Designation,
                    // "changed" = the QMS Password is no longer the date of birth
                    HasCustomPassword = !DobHelper.Matches(e.Password, e.DateOfBirth)
                }).ToList();
            }
            return View(vm);
        }

        // POST: /AdminDashboard/ResetPassword
        // Writes the employee's date of birth (dd/MM/yyyy) back into the QMS Password column.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string employeeNo, string q)
        {
            employeeNo = (employeeNo ?? "").Trim();
            if (employeeNo.Length == 0)
                return RedirectToAction("ResetPasswords");

            var emp = _qms.EmployeeMasters.FirstOrDefault(e => e.EmployeeNo == employeeNo);
            if (emp == null)
            {
                TempData["Error"] = "Employee " + employeeNo + " was not found in the employee master.";
                return RedirectToAction("ResetPasswords", new { q });
            }

            var name = TitleCase(emp.EmployeeName);

            if (DobHelper.Matches(emp.Password, emp.DateOfBirth))
            {
                TempData["Success"] = name + " already uses the default password (date of birth). Any sign-in lock was cleared.";
            }
            else
            {
                var dob = emp.DateOfBirth.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                _qms.SetPassword(emp.EmployeeNo, dob);
                TempData["Success"] = name + "'s password was reset to the date of birth (" + dob + "). Any sign-in lock was cleared.";
            }

            // lift any sign-in / reset lock for this employee
            System.Web.HttpRuntime.Cache.Remove("loginfail:Employee:" + employeeNo.ToLowerInvariant());
            System.Web.HttpRuntime.Cache.Remove("loginfail:Reset:" + employeeNo.ToLowerInvariant());

            return RedirectToAction("ResetPasswords", new { q });
        }

        // ---------------- helpers ----------------

        // Every manager that has a team, biggest team first
        private List<ManagerRow> GetManagerRows()
        {
            var groups = _db.ReportingLines
                .GroupBy(r => r.ManagerEmployeeNo)
                .Select(g => new { Manager = g.Key, Count = g.Count() })
                .ToList();

            var people = Lookup(groups.Select(g => g.Manager));

            return groups
                .Select(g => new ManagerRow
                {
                    EmployeeNo = g.Manager,
                    Name = people[g.Manager].Name,
                    Department = people[g.Manager].Department,
                    Designation = people[g.Manager].Designation,
                    TeamSize = g.Count
                })
                .OrderByDescending(m => m.TeamSize).ThenBy(m => m.Name)
                .ToList();
        }


        // True if employeeNo is the manager, or the manager's manager, and so on (would make a loop)
        private bool WouldCreateLoop(string managerNo, string employeeNo)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = managerNo;

            while (current != null && seen.Add(current))
            {
                if (string.Equals(current, employeeNo, StringComparison.OrdinalIgnoreCase))
                    return true;

                var cur = current;
                current = _db.ReportingLines
                    .Where(r => r.EmployeeNo == cur)
                    .Select(r => r.ManagerEmployeeNo)
                    .FirstOrDefault();
            }
            return false;
        }

        // Search the QMS employee master by name, employee number or department (first 25)
        private List<QmsEmployee> SearchQms(string q)
        {
            return _qms.EmployeeMasters
                .Where(e => e.EmployeeNo.Contains(q) || e.EmployeeName.Contains(q) || e.Department.Contains(q))
                .OrderBy(e => e.EmployeeName)
                .Take(25)
                .ToList();
        }

        // Name / department / designation for employee numbers.
        // QMS first, then the name stored in the punch log, then the number itself.
        private Dictionary<string, EmployeeBrief> Lookup(IEnumerable<string> codes)
        {
            var list = codes.Where(c => !string.IsNullOrEmpty(c))
                            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var result = new Dictionary<string, EmployeeBrief>(StringComparer.OrdinalIgnoreCase);
            if (list.Count == 0) return result;

            foreach (var e in _qms.EmployeeMasters.Where(e => list.Contains(e.EmployeeNo)).ToList())
            {
                result[e.EmployeeNo] = new EmployeeBrief
                {
                    EmployeeNo = e.EmployeeNo,
                    Name = TitleCase(e.EmployeeName),
                    Department = e.Department,
                    Designation = e.Designation
                };
            }

            var missing = list.Where(c => !result.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                var logged = _log.Punches
                    .Where(p => missing.Contains(p.EmpCode))
                    .GroupBy(p => p.EmpCode)
                    .Select(g => new { Code = g.Key, Name = g.Max(x => x.EmplName) })
                    .ToList();

                foreach (var n in logged)
                    result[n.Code] = new EmployeeBrief { EmployeeNo = n.Code, Name = TitleCase(n.Name) };

                foreach (var c in missing.Where(c => !result.ContainsKey(c)))
                    result[c] = new EmployeeBrief { EmployeeNo = c, Name = c };
            }
            return result;
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