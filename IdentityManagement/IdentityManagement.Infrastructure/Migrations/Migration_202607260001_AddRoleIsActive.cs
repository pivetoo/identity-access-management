using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202607260001)]
    public sealed class Migration_202607260001_AddRoleIsActive : Migration
    {
        public override void Up()
        {
            if (!Schema.Table("roles").Exists())
            {
                return;
            }

            if (!Schema.Table("roles").Column("isactive").Exists())
            {
                Alter.Table("roles")
                    .AddColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true);
            }
        }

        public override void Down()
        {
            if (!Schema.Table("roles").Exists())
            {
                return;
            }

            if (Schema.Table("roles").Column("isactive").Exists())
            {
                Delete.Column("isactive").FromTable("roles");
            }
        }
    }
}
