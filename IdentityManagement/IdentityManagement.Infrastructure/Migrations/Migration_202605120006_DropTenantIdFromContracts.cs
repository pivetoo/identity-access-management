using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605120006)]
    public sealed class Migration_202605120006_DropTenantIdFromContracts : Migration
    {
        public override void Up()
        {
            Delete.Column("tenantid").FromTable("contracts");
        }

        public override void Down()
        {
            Alter.Table("contracts")
                .AddColumn("tenantid").AsGuid().Nullable();

            Execute.Sql(@"
                UPDATE contracts c
                SET tenantid = co.tenantid
                FROM companies co
                WHERE co.id = c.companyid;
            ");

            Alter.Table("contracts")
                .AlterColumn("tenantid").AsGuid().NotNullable();
        }
    }
}
