using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeAttendenceManagmentSystem.Models.Qms
{
    // Read-only view of the QMS database table "EmployeeMaster".
    // Only the columns this project needs are mapped; extra columns in the table are ignored.
    [Table("EmployeeMaster")]
    public class QmsEmployee
    {
        [Key]
        public int Id { get; set; }

        public string EmployeeName { get; set; }
        public string EmployeeNo { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public DateTime DateOfBirth { get; set; }

        // Login password stored in QMS (it holds the employee's date of birth)
        public string Password { get; set; }
        public DateTime DateOfRetirement { get; set; }
        public string Status { get; set; }
        public string PhotoPath { get; set; }
    }
}