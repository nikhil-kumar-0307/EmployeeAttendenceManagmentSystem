namespace EmployeeAttendenceManagmentSystem.Migrations
{
    using System.Data.Entity.Migrations;
    using EmployeeAttendenceManagmentSystem.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<AttendanceDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        // Runs after every migration - AddOrUpdate keeps it free of duplicates
        protected override void Seed(AttendanceDbContext context)
        {
            context.Users.AddOrUpdate(u => u.EmployeeCode,
                new User
                {
                    EmployeeCode = "ADMIN01",
                    FullName = "System Admin",
                    Department = "IT",
                    Role = UserRoles.Admin,
                    Password = "Admin@123",
                    IsActive = true
                },
                new User
                {
                    EmployeeCode = "EMP1001",
                    FullName = "Test Employee",
                    Department = "Operations",
                    Role = UserRoles.Employee,
                    Password = "Emp@123",
                    IsActive = true
                });
        }
    }
}