using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604300001)]
    public sealed class Migration_202604300001_RemoveLegacyContractAuthColumns : Migration
    {
        public override void Up()
        {
            if (Schema.Table("contracts").Index("ix_contracts_clientid").Exists())
            {
                Delete.Index("ix_contracts_clientid").OnTable("contracts");
            }

            if (Schema.Table("contracts").Column("clientid").Exists())
            {
                Delete.Column("clientid").FromTable("contracts");
            }

            if (Schema.Table("contracts").Column("clientsecret").Exists())
            {
                Delete.Column("clientsecret").FromTable("contracts");
            }

            if (Schema.Table("contracts").Column("jwtsecretkey").Exists())
            {
                Delete.Column("jwtsecretkey").FromTable("contracts");
            }
        }

        public override void Down()
        {
            if (!Schema.Table("contracts").Column("clientid").Exists())
            {
                Alter.Table("contracts")
                    .AddColumn("clientid").AsString(150).NotNullable().WithDefaultValue(string.Empty);
            }

            if (!Schema.Table("contracts").Column("clientsecret").Exists())
            {
                Alter.Table("contracts")
                    .AddColumn("clientsecret").AsString(255).NotNullable().WithDefaultValue(string.Empty);
            }

            if (!Schema.Table("contracts").Column("jwtsecretkey").Exists())
            {
                Alter.Table("contracts")
                    .AddColumn("jwtsecretkey").AsString(255).NotNullable().WithDefaultValue(string.Empty);
            }
        }
    }
}
