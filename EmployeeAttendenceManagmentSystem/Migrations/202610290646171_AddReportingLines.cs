namespace EmployeeAttendenceManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReportingLines : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.AttendanceRecords", "IX_Employee_Date");
            DropTable("dbo.AttendanceRecords");
        }
        
        public override void Down()
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
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.AttendanceRecords", new[] { "EmployeeNo", "Date" }, unique: true, name: "IX_Employee_Date");
        }
    }
}
