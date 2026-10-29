namespace EmployeeAttendenceManagmentSystem.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LoginTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmployeeCode = c.String(nullable: false, maxLength: 20),
                        FullName = c.String(nullable: false, maxLength: 100),
                        Email = c.String(maxLength: 100),
                        Department = c.String(maxLength: 100),
                        Password = c.String(nullable: false, maxLength: 100),
                        Role = c.String(nullable: false, maxLength: 20),
                        IsActive = c.Boolean(nullable: false),
                        LastLoginAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Users");
        }
    }
}
