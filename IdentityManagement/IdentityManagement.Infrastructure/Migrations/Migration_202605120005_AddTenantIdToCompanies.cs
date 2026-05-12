using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605120005)]
    public sealed class Migration_202605120005_AddTenantIdToCompanies : Migration
    {
        public override void Up()
        {
            Alter.Table("companies")
                .AddColumn("tenantid").AsGuid().Nullable();

            Execute.Sql("UPDATE companies SET tenantid = gen_random_uuid() WHERE tenantid IS NULL;");

            Alter.Table("companies")
                .AlterColumn("tenantid").AsGuid().NotNullable();

            Create.Index("ix_companies_tenantid")
                .OnTable("companies")
                .OnColumn("tenantid").Ascending()
                .WithOptions().Unique();

            Delete.Index("ix_contracts_tenantid").OnTable("contracts");

            Execute.Sql(@"
                UPDATE contracts c
                SET tenantid = co.tenantid
                FROM companies co
                WHERE co.id = c.companyid;
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                UPDATE contracts c
                SET tenantid = gen_random_uuid()
                FROM companies co
                WHERE co.id = c.companyid;
            ");

            Create.Index("ix_contracts_tenantid")
                .OnTable("contracts")
                .OnColumn("tenantid").Ascending()
                .WithOptions().Unique();

            Delete.Index("ix_companies_tenantid").OnTable("companies");

            Delete.Column("tenantid").FromTable("companies");
        }
    }
}
