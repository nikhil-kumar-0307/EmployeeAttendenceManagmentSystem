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

        protected override void Seed(AttendanceDbContext context)
        {
            // Admin login
            context.Users.AddOrUpdate(u => u.EmployeeCode,
                new User
                {
                    EmployeeCode = "ADMIN01",
                    FullName = "System Admin",
                    Department = "IT",
                    Role = UserRoles.Admin,
                    Password = "Admin@123",
                    IsActive = true
                });

            // DUMMY reporting lines: 5866 is the manager of 9001..9005
            // (these match the dummy data in Database/AttendanceLogDb_Setup.sql)
            context.ReportingLines.AddOrUpdate(r => new { r.ManagerEmployeeNo, r.EmployeeNo },
                new ReportingLine { ManagerEmployeeNo = "5866", EmployeeNo = "9001" },
                new ReportingLine { ManagerEmployeeNo = "5866", EmployeeNo = "9002" },
                new ReportingLine { ManagerEmployeeNo = "5866", EmployeeNo = "9003" },
                new ReportingLine { ManagerEmployeeNo = "5866", EmployeeNo = "9004" },
                new ReportingLine { ManagerEmployeeNo = "5866", EmployeeNo = "9005" });
        }
    }
}