using System.ComponentModel.DataAnnotations;

namespace EmployeeAttendenceManagmentSystem.Models.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Enter your Employee ID.")]
        [StringLength(20)]
        [Display(Name = "Username / Emp. Code")]
        public string EmployeeCode { get; set; }

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        // "Employee" or "Admin" - chosen with the toggle on the login card
        [Required]
        public string LoginAs { get; set; } = UserRoles.Employee;

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }
}