using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604030001)]
    public sealed class Migration_202604030001_CreateIdentityManagementTables : Migration
    {
        public override void Up()
        {
            CreateUsersTable();
            CreateCompaniesTable();
            CreateSystemApplicationsTable();
            CreateContractsTable();
            CreateRolesTable();
            CreateAccessResourcesTable();
            CreateUserRolesTable();
            CreateRoleAccessResourcesTable();
            CreateLoginSessionsTable();
            CreateRefreshTokensTable();
            CreateAuthorizationCodesTable();
        }

        public override void Down()
        {
            DeleteAuthorizationCodesTable();
            DeleteRefreshTokensTable();
            DeleteLoginSessionsTable();
            DeleteRoleAccessResourcesTable();
            DeleteUserRolesTable();
            DeleteAccessResourcesTable();
            DeleteRolesTable();
            DeleteContractsTable();
            DeleteSystemApplicationsTable();
            DeleteCompaniesTable();
            DeleteUsersTable();
        }

        private void CreateUsersTable()
        {
            if (!Schema.Table("users").Exists())
            {
                Create.Table("users")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("username").AsString(100).NotNullable()
                    .WithColumn("email").AsString(255).NotNullable()
                    .WithColumn("passwordhash").AsString(255).NotNullable()
                    .WithColumn("name").AsString(200).NotNullable()
                    .WithColumn("avatarurl").AsString(500).Nullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("lastloginat").AsDateTimeOffset().Nullable()
                    .WithColumn("preferredlanguage").AsInt32().NotNullable().WithDefaultValue(1)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("users").Index("ix_users_username").Exists())
            {
                Create.Index("ix_users_username")
                    .OnTable("users")
                    .OnColumn("username").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("users").Index("ix_users_email").Exists())
            {
                Create.Index("ix_users_email")
                    .OnTable("users")
                    .OnColumn("email").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateCompaniesTable()
        {
            if (!Schema.Table("companies").Exists())
            {
                Create.Table("companies")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("legalname").AsString(200).NotNullable()
                    .WithColumn("tradename").AsString(200).NotNullable()
                    .WithColumn("document").AsString(30).NotNullable()
                    .WithColumn("email").AsString(255).NotNullable()
                    .WithColumn("phonenumber").AsString(30).Nullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("companies").Index("ix_companies_document").Exists())
            {
                Create.Index("ix_companies_document")
                    .OnTable("companies")
                    .OnColumn("document").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("companies").Index("ix_companies_email").Exists())
            {
                Create.Index("ix_companies_email")
                    .OnTable("companies")
                    .OnColumn("email").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateSystemApplicationsTable()
        {
            if (!Schema.Table("systemapplications").Exists())
            {
                Create.Table("systemapplications")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("name").AsString(150).NotNullable()
                    .WithColumn("description").AsString(500).NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("audience").AsString(200).NotNullable()
                    .WithColumn("type").AsInt32().NotNullable().WithDefaultValue(2)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("systemapplications").Index("ix_systemapplications_name").Exists())
            {
                Create.Index("ix_systemapplications_name")
                    .OnTable("systemapplications")
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("systemapplications").Index("ix_systemapplications_audience").Exists())
            {
                Create.Index("ix_systemapplications_audience")
                    .OnTable("systemapplications")
                    .OnColumn("audience").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateContractsTable()
        {
            if (!Schema.Table("contracts").Exists())
            {
                Create.Table("contracts")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("companyid").AsInt64().NotNullable()
                    .WithColumn("systemapplicationid").AsInt64().NotNullable()
                    .WithColumn("startdate").AsDateTimeOffset().NotNullable()
                    .WithColumn("enddate").AsDateTimeOffset().Nullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("clientid").AsString(150).NotNullable()
                    .WithColumn("clientsecret").AsString(255).NotNullable()
                    .WithColumn("accesstokenlifetime").AsInt32().NotNullable()
                    .WithColumn("refreshtokenlifetime").AsInt32().NotNullable()
                    .WithColumn("jwtsecretkey").AsString(255).NotNullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("contracts").Constraint("fk_contracts_companies_companyid").Exists())
            {
                Create.ForeignKey("fk_contracts_companies_companyid")
                    .FromTable("contracts").ForeignColumn("companyid")
                    .ToTable("companies").PrimaryColumn("id");
            }

            if (!Schema.Table("contracts").Constraint("fk_contracts_systemapplications_systemapplicationid").Exists())
            {
                Create.ForeignKey("fk_contracts_systemapplications_systemapplicationid")
                    .FromTable("contracts").ForeignColumn("systemapplicationid")
                    .ToTable("systemapplications").PrimaryColumn("id");
            }

            if (!Schema.Table("contracts").Index("ix_contracts_clientid").Exists())
            {
                Create.Index("ix_contracts_clientid")
                    .OnTable("contracts")
                    .OnColumn("clientid").Ascending()
                    .WithOptions().Unique();
            }

            if (!Schema.Table("contracts").Index("ix_contracts_companyid_systemapplicationid").Exists())
            {
                Create.Index("ix_contracts_companyid_systemapplicationid")
                    .OnTable("contracts")
                    .OnColumn("companyid").Ascending()
                    .OnColumn("systemapplicationid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateRolesTable()
        {
            if (!Schema.Table("roles").Exists())
            {
                Create.Table("roles")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("name").AsString(120).NotNullable()
                    .WithColumn("description").AsString(500).NotNullable()
                    .WithColumn("contractid").AsInt64().NotNullable()
                    .WithColumn("isroot").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("isdefault").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("roles").Constraint("fk_roles_contracts_contractid").Exists())
            {
                Create.ForeignKey("fk_roles_contracts_contractid")
                    .FromTable("roles").ForeignColumn("contractid")
                    .ToTable("contracts").PrimaryColumn("id");
            }

            if (!Schema.Table("roles").Index("ix_roles_contractid_name").Exists())
            {
                Create.Index("ix_roles_contractid_name")
                    .OnTable("roles")
                    .OnColumn("contractid").Ascending()
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateAccessResourcesTable()
        {
            if (!Schema.Table("accessresources").Exists())
            {
                Create.Table("accessresources")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("name").AsString(200).NotNullable()
                    .WithColumn("controller").AsString(120).NotNullable()
                    .WithColumn("action").AsString(120).NotNullable()
                    .WithColumn("httpmethod").AsString(20).NotNullable()
                    .WithColumn("route").AsString(500).NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("accessresources").Index("ix_accessresources_name").Exists())
            {
                Create.Index("ix_accessresources_name")
                    .OnTable("accessresources")
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateUserRolesTable()
        {
            if (!Schema.Table("userroles").Exists())
            {
                Create.Table("userroles")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("userid").AsInt64().NotNullable()
                    .WithColumn("roleid").AsInt64().NotNullable()
                    .WithColumn("assignedat").AsDateTimeOffset().NotNullable()
                    .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("userroles").Constraint("fk_userroles_users_userid").Exists())
            {
                Create.ForeignKey("fk_userroles_users_userid")
                    .FromTable("userroles").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");
            }

            if (!Schema.Table("userroles").Constraint("fk_userroles_roles_roleid").Exists())
            {
                Create.ForeignKey("fk_userroles_roles_roleid")
                    .FromTable("userroles").ForeignColumn("roleid")
                    .ToTable("roles").PrimaryColumn("id");
            }

            if (!Schema.Table("userroles").Index("ix_userroles_userid_roleid").Exists())
            {
                Create.Index("ix_userroles_userid_roleid")
                    .OnTable("userroles")
                    .OnColumn("userid").Ascending()
                    .OnColumn("roleid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateRoleAccessResourcesTable()
        {
            if (!Schema.Table("roleaccessresources").Exists())
            {
                Create.Table("roleaccessresources")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("roleid").AsInt64().NotNullable()
                    .WithColumn("accessresourceid").AsInt64().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("roleaccessresources").Constraint("fk_roleaccessresources_roles_roleid").Exists())
            {
                Create.ForeignKey("fk_roleaccessresources_roles_roleid")
                    .FromTable("roleaccessresources").ForeignColumn("roleid")
                    .ToTable("roles").PrimaryColumn("id");
            }

            if (!Schema.Table("roleaccessresources").Constraint("fk_roleaccessresources_accessresources_accessresourceid").Exists())
            {
                Create.ForeignKey("fk_roleaccessresources_accessresources_accessresourceid")
                    .FromTable("roleaccessresources").ForeignColumn("accessresourceid")
                    .ToTable("accessresources").PrimaryColumn("id");
            }

            if (!Schema.Table("roleaccessresources").Index("ix_roleaccessresources_roleid_accessresourceid").Exists())
            {
                Create.Index("ix_roleaccessresources_roleid_accessresourceid")
                    .OnTable("roleaccessresources")
                    .OnColumn("roleid").Ascending()
                    .OnColumn("accessresourceid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateLoginSessionsTable()
        {
            if (!Schema.Table("loginsessions").Exists())
            {
                Create.Table("loginsessions")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("userid").AsInt64().NotNullable()
                    .WithColumn("contractid").AsInt64().NotNullable()
                    .WithColumn("sessionid").AsString(64).NotNullable()
                    .WithColumn("ipaddress").AsString(45).Nullable()
                    .WithColumn("useragent").AsString(1000).Nullable()
                    .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("loginsessions").Constraint("fk_loginsessions_users_userid").Exists())
            {
                Create.ForeignKey("fk_loginsessions_users_userid")
                    .FromTable("loginsessions").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");
            }

            if (!Schema.Table("loginsessions").Constraint("fk_loginsessions_contracts_contractid").Exists())
            {
                Create.ForeignKey("fk_loginsessions_contracts_contractid")
                    .FromTable("loginsessions").ForeignColumn("contractid")
                    .ToTable("contracts").PrimaryColumn("id");
            }

            if (!Schema.Table("loginsessions").Index("ix_loginsessions_sessionid").Exists())
            {
                Create.Index("ix_loginsessions_sessionid")
                    .OnTable("loginsessions")
                    .OnColumn("sessionid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateRefreshTokensTable()
        {
            if (!Schema.Table("refreshtokens").Exists())
            {
                Create.Table("refreshtokens")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("token").AsString(512).NotNullable()
                    .WithColumn("userid").AsInt64().NotNullable()
                    .WithColumn("contractid").AsInt64().Nullable()
                    .WithColumn("sessionid").AsString(64).NotNullable()
                    .WithColumn("scopes").AsString(2000).NotNullable()
                    .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                    .WithColumn("isrevoked").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                    .WithColumn("lastusedat").AsDateTimeOffset().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("refreshtokens").Constraint("fk_refreshtokens_users_userid").Exists())
            {
                Create.ForeignKey("fk_refreshtokens_users_userid")
                    .FromTable("refreshtokens").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");
            }

            if (!Schema.Table("refreshtokens").Constraint("fk_refreshtokens_contracts_contractid").Exists())
            {
                Create.ForeignKey("fk_refreshtokens_contracts_contractid")
                    .FromTable("refreshtokens").ForeignColumn("contractid")
                    .ToTable("contracts").PrimaryColumn("id");
            }

            if (!Schema.Table("refreshtokens").Index("ix_refreshtokens_token").Exists())
            {
                Create.Index("ix_refreshtokens_token")
                    .OnTable("refreshtokens")
                    .OnColumn("token").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateAuthorizationCodesTable()
        {
            if (!Schema.Table("authorizationcodes").Exists())
            {
                Create.Table("authorizationcodes")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("code").AsString(512).NotNullable()
                    .WithColumn("userid").AsInt64().NotNullable()
                    .WithColumn("contractid").AsInt64().Nullable()
                    .WithColumn("scopes").AsString(2000).NotNullable()
                    .WithColumn("redirecturi").AsString(2000).NotNullable()
                    .WithColumn("sessionid").AsString(64).NotNullable()
                    .WithColumn("expiresat").AsDateTimeOffset().NotNullable()
                    .WithColumn("isused").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("isrevoked").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("success").AsBoolean().NotNullable().WithDefaultValue(false)
                    .WithColumn("tokenexpiration").AsDateTimeOffset().Nullable()
                    .WithColumn("usedat").AsDateTimeOffset().Nullable()
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("authorizationcodes").Constraint("fk_authorizationcodes_users_userid").Exists())
            {
                Create.ForeignKey("fk_authorizationcodes_users_userid")
                    .FromTable("authorizationcodes").ForeignColumn("userid")
                    .ToTable("users").PrimaryColumn("id");
            }

            if (!Schema.Table("authorizationcodes").Constraint("fk_authorizationcodes_contracts_contractid").Exists())
            {
                Create.ForeignKey("fk_authorizationcodes_contracts_contractid")
                    .FromTable("authorizationcodes").ForeignColumn("contractid")
                    .ToTable("contracts").PrimaryColumn("id");
            }

            if (!Schema.Table("authorizationcodes").Index("ix_authorizationcodes_code").Exists())
            {
                Create.Index("ix_authorizationcodes_code")
                    .OnTable("authorizationcodes")
                    .OnColumn("code").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void DeleteAuthorizationCodesTable()
        {
            if (!Schema.Table("authorizationcodes").Exists())
            {
                return;
            }

            if (Schema.Table("authorizationcodes").Index("ix_authorizationcodes_code").Exists())
            {
                Delete.Index("ix_authorizationcodes_code").OnTable("authorizationcodes");
            }

            if (Schema.Table("authorizationcodes").Constraint("fk_authorizationcodes_contracts_contractid").Exists())
            {
                Delete.ForeignKey("fk_authorizationcodes_contracts_contractid").OnTable("authorizationcodes");
            }

            if (Schema.Table("authorizationcodes").Constraint("fk_authorizationcodes_users_userid").Exists())
            {
                Delete.ForeignKey("fk_authorizationcodes_users_userid").OnTable("authorizationcodes");
            }

            Delete.Table("authorizationcodes");
        }

        private void DeleteRefreshTokensTable()
        {
            if (!Schema.Table("refreshtokens").Exists())
            {
                return;
            }

            if (Schema.Table("refreshtokens").Index("ix_refreshtokens_token").Exists())
            {
                Delete.Index("ix_refreshtokens_token").OnTable("refreshtokens");
            }

            if (Schema.Table("refreshtokens").Constraint("fk_refreshtokens_contracts_contractid").Exists())
            {
                Delete.ForeignKey("fk_refreshtokens_contracts_contractid").OnTable("refreshtokens");
            }

            if (Schema.Table("refreshtokens").Constraint("fk_refreshtokens_users_userid").Exists())
            {
                Delete.ForeignKey("fk_refreshtokens_users_userid").OnTable("refreshtokens");
            }

            Delete.Table("refreshtokens");
        }

        private void DeleteLoginSessionsTable()
        {
            if (!Schema.Table("loginsessions").Exists())
            {
                return;
            }

            if (Schema.Table("loginsessions").Index("ix_loginsessions_sessionid").Exists())
            {
                Delete.Index("ix_loginsessions_sessionid").OnTable("loginsessions");
            }

            if (Schema.Table("loginsessions").Constraint("fk_loginsessions_contracts_contractid").Exists())
            {
                Delete.ForeignKey("fk_loginsessions_contracts_contractid").OnTable("loginsessions");
            }

            if (Schema.Table("loginsessions").Constraint("fk_loginsessions_users_userid").Exists())
            {
                Delete.ForeignKey("fk_loginsessions_users_userid").OnTable("loginsessions");
            }

            Delete.Table("loginsessions");
        }

        private void DeleteRoleAccessResourcesTable()
        {
            if (!Schema.Table("roleaccessresources").Exists())
            {
                return;
            }

            if (Schema.Table("roleaccessresources").Index("ix_roleaccessresources_roleid_accessresourceid").Exists())
            {
                Delete.Index("ix_roleaccessresources_roleid_accessresourceid").OnTable("roleaccessresources");
            }

            if (Schema.Table("roleaccessresources").Constraint("fk_roleaccessresources_accessresources_accessresourceid").Exists())
            {
                Delete.ForeignKey("fk_roleaccessresources_accessresources_accessresourceid").OnTable("roleaccessresources");
            }

            if (Schema.Table("roleaccessresources").Constraint("fk_roleaccessresources_roles_roleid").Exists())
            {
                Delete.ForeignKey("fk_roleaccessresources_roles_roleid").OnTable("roleaccessresources");
            }

            Delete.Table("roleaccessresources");
        }

        private void DeleteUserRolesTable()
        {
            if (!Schema.Table("userroles").Exists())
            {
                return;
            }

            if (Schema.Table("userroles").Index("ix_userroles_userid_roleid").Exists())
            {
                Delete.Index("ix_userroles_userid_roleid").OnTable("userroles");
            }

            if (Schema.Table("userroles").Constraint("fk_userroles_roles_roleid").Exists())
            {
                Delete.ForeignKey("fk_userroles_roles_roleid").OnTable("userroles");
            }

            if (Schema.Table("userroles").Constraint("fk_userroles_users_userid").Exists())
            {
                Delete.ForeignKey("fk_userroles_users_userid").OnTable("userroles");
            }

            Delete.Table("userroles");
        }

        private void DeleteAccessResourcesTable()
        {
            if (!Schema.Table("accessresources").Exists())
            {
                return;
            }

            if (Schema.Table("accessresources").Index("ix_accessresources_systemapplicationid_name").Exists())
            {
                Delete.Index("ix_accessresources_systemapplicationid_name").OnTable("accessresources");
            }

            if (Schema.Table("accessresources").Index("ix_accessresources_name").Exists())
            {
                Delete.Index("ix_accessresources_name").OnTable("accessresources");
            }

            if (Schema.Table("accessresources").Constraint("fk_accessresources_systemapplications_systemapplicationid").Exists())
            {
                Delete.ForeignKey("fk_accessresources_systemapplications_systemapplicationid").OnTable("accessresources");
            }

            Delete.Table("accessresources");
        }

        private void DeleteRolesTable()
        {
            if (!Schema.Table("roles").Exists())
            {
                return;
            }

            if (Schema.Table("roles").Index("ix_roles_contractid_name").Exists())
            {
                Delete.Index("ix_roles_contractid_name").OnTable("roles");
            }

            if (Schema.Table("roles").Constraint("fk_roles_contracts_contractid").Exists())
            {
                Delete.ForeignKey("fk_roles_contracts_contractid").OnTable("roles");
            }

            Delete.Table("roles");
        }

        private void DeleteContractsTable()
        {
            if (!Schema.Table("contracts").Exists())
            {
                return;
            }

            if (Schema.Table("contracts").Index("ix_contracts_companyid_systemapplicationid").Exists())
            {
                Delete.Index("ix_contracts_companyid_systemapplicationid").OnTable("contracts");
            }

            if (Schema.Table("contracts").Index("ix_contracts_clientid").Exists())
            {
                Delete.Index("ix_contracts_clientid").OnTable("contracts");
            }

            if (Schema.Table("contracts").Constraint("fk_contracts_systemapplications_systemapplicationid").Exists())
            {
                Delete.ForeignKey("fk_contracts_systemapplications_systemapplicationid").OnTable("contracts");
            }

            if (Schema.Table("contracts").Constraint("fk_contracts_companies_companyid").Exists())
            {
                Delete.ForeignKey("fk_contracts_companies_companyid").OnTable("contracts");
            }

            Delete.Table("contracts");
        }

        private void DeleteSystemApplicationsTable()
        {
            if (!Schema.Table("systemapplications").Exists())
            {
                return;
            }

            if (Schema.Table("systemapplications").Index("ix_systemapplications_audience").Exists())
            {
                Delete.Index("ix_systemapplications_audience").OnTable("systemapplications");
            }

            if (Schema.Table("systemapplications").Index("ix_systemapplications_name").Exists())
            {
                Delete.Index("ix_systemapplications_name").OnTable("systemapplications");
            }

            Delete.Table("systemapplications");
        }

        private void DeleteCompaniesTable()
        {
            if (!Schema.Table("companies").Exists())
            {
                return;
            }

            if (Schema.Table("companies").Index("ix_companies_email").Exists())
            {
                Delete.Index("ix_companies_email").OnTable("companies");
            }

            if (Schema.Table("companies").Index("ix_companies_document").Exists())
            {
                Delete.Index("ix_companies_document").OnTable("companies");
            }

            Delete.Table("companies");
        }

        private void DeleteUsersTable()
        {
            if (!Schema.Table("users").Exists())
            {
                return;
            }

            if (Schema.Table("users").Index("ix_users_email").Exists())
            {
                Delete.Index("ix_users_email").OnTable("users");
            }

            if (Schema.Table("users").Index("ix_users_username").Exists())
            {
                Delete.Index("ix_users_username").OnTable("users");
            }

            Delete.Table("users");
        }
    }
}
