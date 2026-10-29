namespace EmployeeAttendenceManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAttendanceAndReporting : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AttendanceRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmployeeNo = c.String(nullable: false, maxLength: 20),
                        Date = c.DateTime(nullable: false, storeType: "date"),
                        InTime = c.Time(precision: 7),
                        OutTime = c.Time(precision: 7),
                        Status = c.String(nullable: false, maxLength: 20),
                        Remarks = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.EmployeeNo, t.Date }, unique: true, name: "IX_Employee_Date");
            
            CreateTable(
                "dbo.ReportingLines",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ManagerEmployeeNo = c.String(nullable: false, maxLength: 20),
                        EmployeeNo = c.String(nullable: false, maxLength: 20),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => new { t.ManagerEmployeeNo, t.EmployeeNo }, unique: true, name: "IX_Manager_Employee");
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.ReportingLines", "IX_Manager_Employee");
            DropIndex("dbo.AttendanceRecords", "IX_Employee_Date");
            DropTable("dbo.ReportingLines");
            DropTable("dbo.AttendanceRecords");
        }
    }
}
