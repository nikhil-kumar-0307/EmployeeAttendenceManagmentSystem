using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using EmployeeAttendenceManagmentSystem.Models;
using EmployeeAttendenceManagmentSystem.Models.DTOs;

namespace EmployeeAttendenceManagmentSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AttendanceDbContext _db = new AttendanceDbContext();

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
            var user = _db.Users.FirstOrDefault(u => u.EmployeeCode == code);

            // One generic message so we don't reveal which part was wrong
            bool valid = user != null
                         && user.IsActive
                         && user.Password == model.Password
                         && string.Equals(user.Role, model.LoginAs, StringComparison.OrdinalIgnoreCase);

            if (!valid)
            {
                ModelState.AddModelError("", "Invalid credentials for the selected login type.");
                model.Password = null;
                return View(model);
            }

            user.LastLoginAt = DateTime.Now;
            _db.SaveChanges();

            FormsAuthentication.SetAuthCookie(user.EmployeeCode, model.RememberMe);
            Session["UserId"] = user.Id;
            Session["EmployeeCode"] = user.EmployeeCode;
            Session["FullName"] = user.FullName;
            Session["Role"] = user.Role;

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToDashboard(user.Role);
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

        private ActionResult RedirectToDashboard(string role)
        {
            // Create these controllers/views next
            if (role == UserRoles.Admin)
                return RedirectToAction("Index", "AdminDashboard");

            return RedirectToAction("Index", "EmployeeDashboard");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}