using System.ComponentModel.DataAnnotations;

namespace EmployeeAttendenceManagmentSystem.Models.DTOs
{
    // Signed-in user changes their own password
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Enter your current password.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Enter a new password.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "The new password must be 6 to 50 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Type the new password again.")]
        [Compare("NewPassword", ErrorMessage = "The two passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }

    // Employee who forgot the password: proves identity with employee number + date of birth
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Enter your employee number.")]
        [StringLength(20)]
        public string EmployeeNo { get; set; }

        [Required(ErrorMessage = "Enter your date of birth.")]
        public string DateOfBirth { get; set; }

        [Required(ErrorMessage = "Enter a new password.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "The new password must be 6 to 50 characters.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Type the new password again.")]
        [Compare("NewPassword", ErrorMessage = "The two passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}