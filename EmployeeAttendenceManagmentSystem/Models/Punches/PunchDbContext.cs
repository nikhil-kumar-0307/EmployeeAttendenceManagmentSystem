using System;
using System.Data.Entity;

namespace EmployeeAttendenceManagmentSystem.Models.Punches
{
    // Read-only connection to the attendance punch database (connection string: "AttendanceLogDb")
    public class PunchDbContext : DbContext
    {
        public PunchDbContext() : base("name=AttendanceLogDb")
        {
            Database.SetInitializer<PunchDbContext>(null);   // never create / migrate that database
        }

        public DbSet<PunchRecord> Punches { get; set; }

        public override int SaveChanges()
        {
            throw new InvalidOperationException("The attendance log database is read-only in this application.");
        }
    }
}