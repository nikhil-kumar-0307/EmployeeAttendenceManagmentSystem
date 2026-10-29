using System.Data.Entity;

namespace EmployeeAttendenceManagmentSystem.Models
{
    public class AttendanceDbContext : DbContext
    {
        // Connection string name in Web.config: "AttendanceDb"
        public AttendanceDbContext() : base("name=AttendanceDb")
        {
            // Applies pending migrations and runs Migrations/Configuration.cs Seed on startup
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<AttendanceDbContext, Migrations.Configuration>());
        }

        public DbSet<User> Users { get; set; }
    }
}