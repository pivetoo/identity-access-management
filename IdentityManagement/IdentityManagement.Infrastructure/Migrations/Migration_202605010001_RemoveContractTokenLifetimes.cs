using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605010001)]
    public sealed class Migration_202605010001_RemoveContractTokenLifetimes : Migration
    {
        public override void Up()
        {
            if (Schema.Table("contracts").Column("accesstokenlifetime").Exists())
            {
                Delete.Column("accesstokenlifetime").FromTable("contracts");
            }

            if (Schema.Table("contracts").Column("refreshtokenlifetime").Exists())
            {
                Delete.Column("refreshtokenlifetime").FromTable("contracts");
            }
        }

        public override void Down()
        {
            if (!Schema.Table("contracts").Column("accesstokenlifetime").Exists())
            {
                Alter.Table("contracts")
                    .AddColumn("accesstokenlifetime").AsInt32().NotNullable().WithDefaultValue(3600);
            }

            if (!Schema.Table("contracts").Column("refreshtokenlifetime").Exists())
            {
                Alter.Table("contracts")
                    .AddColumn("refreshtokenlifetime").AsInt32().NotNullable().WithDefaultValue(2592000);
            }
        }
    }
}
