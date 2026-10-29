using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeAttendenceManagmentSystem.Models.Punches
{
    // One row = one punch (swipe) of one employee.
    // If your real column names differ, change ONLY the [Column("...")] names below.
    [Table("AttendanceLog")]
    public class PunchRecord
    {
        [Key]
        [Column("Recid")]
        public int Recid { get; set; }

        [Column("empCode")]
        public string EmpCode { get; set; }

        [Column("empl_name")]
        public string EmplName { get; set; }

        [Column("TransactionDateTime")]
        public DateTime TransactionDateTime { get; set; }
    }
}