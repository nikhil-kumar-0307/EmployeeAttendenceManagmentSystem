using System;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAttendenceManagmentSystem.Models
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string EmployeeCode { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(100)]
        public string Department { get; set; }

        // Stored as plain text (as requested)
        [Required, StringLength(100)]
        public string Password { get; set; }

        // "Admin" or "Employee"
        [Required, StringLength(20)]
        public string Role { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }
    }
}