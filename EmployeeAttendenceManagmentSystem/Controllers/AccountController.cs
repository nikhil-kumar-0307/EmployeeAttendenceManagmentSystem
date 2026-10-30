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

        private readonly AttendanceDbContext _db = new AttendanceDbContext();   // admins, password overrides
        private readonly QmsDbContext _qms = new QmsDbContext();                // employees live in QMS

        // =====================================================================
        //  SIGN IN / SIGN OUT
        // =====================================================================

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
                    : "Invalid employee number or password.");
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

        // =====================================================================
        //  CHANGE PASSWORD  (signed-in Admin or Employee)
        // =====================================================================

        // GET: /Account/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            var redirect = RequireSignIn();
            if (redirect != null) return redirect;

            return View(new ChangePasswordDto());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordDto model)
        {
            var redirect = RequireSignIn();
            if (redirect != null) return redirect;

            if (!ModelState.IsValid)
                return View(model);

            var code = (string)Session["EmployeeCode"];
            var role = (string)Session["Role"];
            var current = model.CurrentPassword.Trim();
            var newPassword = model.NewPassword.Trim();

            if (IsLockedOut("Change", code))
            {
                ModelState.AddModelError("", "Too many wrong attempts. Try again in a few minutes.");
                return View(model);
            }

            if (string.Equals(current, newPassword, StringComparison.Ordinal))
            {
                ModelState.AddModelError("", "The new password must be different from the current one.");
                return View(model);
            }

            bool currentOk;
            if (role == UserRoles.Admin)
            {
                var admin = _db.Users.FirstOrDefault(u =>
                    u.EmployeeCode == code && u.Role == UserRoles.Admin && u.IsActive);

                currentOk = admin != null && admin.Password == model.CurrentPassword;
                if (currentOk)
                {
                    admin.Password = newPassword;
                    _db.SaveChanges();
                }
            }
            else
            {
                var emp = _qms.EmployeeMasters.FirstOrDefault(e => e.EmployeeNo == code);
                currentOk = emp != null && DobHelper.PasswordMatches(model.CurrentPassword, emp.Password);
                if (currentOk)
                    _qms.SetPassword(emp.EmployeeNo, newPassword);       // writes the QMS Password column
            }

            if (!currentOk)
            {
                RegisterFailure("Change", code);
                ModelState.AddModelError("", "The current password is not correct.");
                return View(model);
            }

            ClearFailures("Change", code);
            TempData["Success"] = "Your password was changed. Use the new password the next time you sign in.";
            return RedirectToAction("ChangePassword");
        }

        // =====================================================================
        //  FORGOT PASSWORD  (employee, from the sign-in page)
        // =====================================================================

        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordDto());
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var code = model.EmployeeNo.Trim();

            if (IsLockedOut("Reset", code))
            {
                ModelState.AddModelError("", "Too many failed attempts. Try again in a few minutes.");
                return View(model);
            }

            // identity check: the date of birth must match the one in QMS
            var emp = _qms.EmployeeMasters.FirstOrDefault(e => e.EmployeeNo == code);
            if (emp == null || !DobHelper.Matches(model.DateOfBirth, emp.DateOfBirth))
            {
                RegisterFailure("Reset", code);
                ModelState.AddModelError("", "We could not verify these details. Check the employee number and date of birth.");
                return View(model);
            }

            _qms.SetPassword(emp.EmployeeNo, model.NewPassword.Trim());   // writes the QMS Password column

            ClearFailures("Reset", code);
            ClearFailures(UserRoles.Employee, code);   // also lift any sign-in lock

            TempData["Success"] = "Your password was reset. Sign in with your new password.";
            return RedirectToAction("Login");
        }

        // =====================================================================
        //  helpers
        // =====================================================================

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

        // ---------- Employee: QMS EmployeeMaster (EmployeeNo + Password column) ----------
        private bool TryEmployeeLogin(string employeeNo, string typedPassword,
                                      out int id, out string name, out string department)
        {
            id = 0; name = null; department = null;

            var emp = _qms.EmployeeMasters.FirstOrDefault(e => e.EmployeeNo == employeeNo);

            if (emp == null || !DobHelper.PasswordMatches(typedPassword, emp.Password))
                return false;

            id = emp.Id;
            name = emp.EmployeeName;
            department = emp.Department;
            return true;
        }

        private ActionResult RequireSignIn()
        {
            if (Session["EmployeeCode"] == null)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login");
            }
            return null;
        }

        // ---------- Simple lockout ----------
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