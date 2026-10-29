using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeAttendenceManagmentSystem.Models
{    // One row per employee per day. EmployeeNo matches EmployeeMaster.EmployeeNo in QMS.
    public class AttendanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Index("IX_Employee_Date", 1, IsUnique = true)]
        public string EmployeeNo { get; set; }

        [Column(TypeName = "date")]
        [Index("IX_Employee_Date", 2, IsUnique = true)]
        public DateTime Date { get; set; }

        public TimeSpan? InTime { get; set; }
        public TimeSpan? OutTime { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; }

        [StringLength(200)]
        public string Remarks { get; set; }
    }
}