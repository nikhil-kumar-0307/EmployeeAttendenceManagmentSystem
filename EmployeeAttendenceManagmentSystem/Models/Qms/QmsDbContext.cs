using System;
using System.Data.Entity;

namespace EmployeeAttendenceManagmentSystem.Models.Qms
{
    public class QmsDbContext : DbContext
    {
        public QmsDbContext() : base("name=QmsDb")
        {
            Database.SetInitializer<QmsDbContext>(null);   // no create / migrate / seed
        }

        public DbSet<QmsEmployee> EmployeeMasters { get; set; }

        public override int SaveChanges()
        {
            throw new InvalidOperationException("The QMS database is read-only in this application.");
        }
    }
}