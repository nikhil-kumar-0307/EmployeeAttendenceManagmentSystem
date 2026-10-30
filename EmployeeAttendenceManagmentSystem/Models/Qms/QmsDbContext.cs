using System;
using System.Data.Entity;

namespace EmployeeAttendenceManagmentSystem.Models.Qms
{
    // Connection to the QMS database (connection string name: "QmsDb").
    // EF never creates or migrates it, and SaveChanges is blocked.
    // The ONLY change this application makes there is SetPassword() below.
    public class QmsDbContext : DbContext
    {
        public QmsDbContext() : base("name=QmsDb")
        {
            Database.SetInitializer<QmsDbContext>(null);   // no create / migrate / seed
        }

        public DbSet<QmsEmployee> EmployeeMasters { get; set; }

        public override int SaveChanges()
        {
            throw new InvalidOperationException("The QMS database is read-only in this application, except SetPassword().");
        }

        // Updates the Password column of one employee in QMS (parameterised, one row).
        // Returns the number of rows changed (1 when the employee exists).
        public int SetPassword(string employeeNo, string newPassword)
        {
            return Database.ExecuteSqlCommand(
                "UPDATE [dbo].[EmployeeMaster] SET [Password] = @p0 WHERE [EmployeeNo] = @p1",
                newPassword, employeeNo);
        }
    }
}