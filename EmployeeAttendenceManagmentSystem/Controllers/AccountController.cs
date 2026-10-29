using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using EmployeeAttendenceManagmentSystem.Helpers;
using EmployeeAttendenceManagmentSystem.Models;
using EmployeeAttendenceManagmentSystem.Models.DTOs;
using EmployeeAttendenceManagmentSystem.Models.Qms;

namespace EmployeeAttendenceManagmentSystem.Controllers
{
    public class AccountController : Controller
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(10);

        private readonly AttendanceDbContext _db = new AttendanceDbContext();   // admins live here
        private readonly QmsDbContext _qms = new QmsDbContext();                // employees live in QMS

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToDashboard(Session["Role"] as string);

            return View(new LoginDto { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var code = model.EmployeeCode.Trim();
            bool asAdmin = string.Equals(model.LoginAs, UserRoles.Admin, StringComparison.OrdinalIgnoreCase);
            string role = asAdmin ? UserRoles.Admin : UserRoles.Employee;

            if (IsLockedOut(role, code))
            {
                ModelState.AddModelError("", "Too many failed attempts. Try again in a few minutes.");
                model.Password = null;
                return View(model);
            }

            int userId;
            string fullName, department = null;
            bool valid = asAdmin
                ? TryAdminLogin(code, model.Password, out userId, out fullName)
                : TryEmployeeLogin(code, model.Password, out userId, out fullName, out department);

            if (!valid)
            {
                RegisterFailure(role, code);
                ModelState.AddModelError("", asAdmin
                    ? "Invalid admin ID or password."
                    : "Invalid employee number or date of birth.");
                model.Password = null;
                return View(model);
            }

            ClearFailures(role, code);

            FormsAuthentication.SetAuthCookie(code, model.RememberMe);
            Session["UserId"] = userId;
            Session["EmployeeCode"] = code;
            Session["FullName"] = fullName;
            Session["Department"] = department;
            Session["Role"] = role;

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToDashboard(role);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // ---------- Admin: Users table in the attendance database ----------
        private bool TryAdminLogin(string code, string password, out int id, out string name)
        {
            id = 0; name = null;

            var admin = _db.Users.FirstOrDefault(u =>
                u.EmployeeCode == code && u.Role == UserRoles.Admin && u.IsActive);

            if (admin == null || admin.Password != password)
                return false;

            admin.LastLoginAt = DateTime.Now;
            _db.SaveChanges();

            id = admin.Id;
            name = admin.FullName;
            return true;
        }

        // ---------- Employee: EmployeeMaster table in the QMS database ----------
        // Username = EmployeeNo, Password = Date of Birth
        private bool TryEmployeeLogin(string employeeNo, string typedDob,
                                      out int id, out string name, out string department)
        {
            id = 0; name = null; department = null;

            var emp = _qms.EmployeeMasters.FirstOrDefault(e => e.EmployeeNo == employeeNo);

            if (emp == null || !DobHelper.Matches(typedDob, emp.DateOfBirth))
                return false;

            id = emp.Id;
            name = emp.EmployeeName;
            department = emp.Department;
            return true;
        }

        // ---------- Simple lockout (a date of birth is easy to guess) ----------
        private static string FailKey(string role, string code)
        {
            return "loginfail:" + role + ":" + code.ToLowerInvariant();
        }

        private static bool IsLockedOut(string role, string code)
        {
            var count = HttpRuntime.Cache[FailKey(role, code)] as int?;
            return count.HasValue && count.Value >= MaxFailedAttempts;
        }

        private static void RegisterFailure(string role, string code)
        {
            var key = FailKey(role, code);
            int count = (HttpRuntime.Cache[key] as int?) ?? 0;
            HttpRuntime.Cache.Insert(key, count + 1, null,
                DateTime.UtcNow.Add(LockoutWindow), System.Web.Caching.Cache.NoSlidingExpiration);
        }

        private static void ClearFailures(string role, string code)
        {
            HttpRuntime.Cache.Remove(FailKey(role, code));
        }

        private ActionResult RedirectToDashboard(string role)
        {
            // Create these controllers/views next
            if (role == UserRoles.Admin)
                return RedirectToAction("Index", "AdminDashboard");

            return RedirectToAction("Index", "EmployeeDashboard");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
                _qms.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}