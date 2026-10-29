using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeAttendenceManagmentSystem.Models
{
    // "EmployeeNo reports to ManagerEmployeeNo". One row per subordinate.
    public class ReportingLine
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Index("IX_Manager_Employee", 1, IsUnique = true)]
        public string ManagerEmployeeNo { get; set; }

        [Required, StringLength(20)]
        [Index("IX_Manager_Employee", 2, IsUnique = true)]
        public string EmployeeNo { get; set; }
    }
}