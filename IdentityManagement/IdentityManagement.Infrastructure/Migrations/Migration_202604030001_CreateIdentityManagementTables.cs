using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604030001)]
    public sealed class Migration_202604030001_CreateIdentityManagementTables : Migration
    {
        public override void Up()
        {
            Create.Table("Users")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Username").AsString(100).NotNullable()
                .WithColumn("Email").AsString(255).NotNullable()
                .WithColumn("PasswordHash").AsString(255).NotNullable()
                .WithColumn("Name").AsString(200).NotNullable()
                .WithColumn("AvatarUrl").AsString(500).Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("LastLoginAt").AsDateTimeOffset().Nullable()
                .WithColumn("PreferredLanguage").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.Index("IX_Users_Username")
                .OnTable("Users")
                .OnColumn("Username").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_Users_Email")
                .OnTable("Users")
                .OnColumn("Email").Ascending()
                .WithOptions().Unique();

            Create.Table("Companies")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("LegalName").AsString(200).NotNullable()
                .WithColumn("TradeName").AsString(200).NotNullable()
                .WithColumn("Document").AsString(30).NotNullable()
                .WithColumn("Email").AsString(255).NotNullable()
                .WithColumn("PhoneNumber").AsString(30).Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.Index("IX_Companies_Document")
                .OnTable("Companies")
                .OnColumn("Document").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_Companies_Email")
                .OnTable("Companies")
                .OnColumn("Email").Ascending()
                .WithOptions().Unique();

            Create.Table("SystemApplications")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Name").AsString(150).NotNullable()
                .WithColumn("Description").AsString(500).NotNullable()
                .WithColumn("RedirectUris").AsString(2000).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Audience").AsString(200).NotNullable()
                .WithColumn("Type").AsInt32().NotNullable().WithDefaultValue(2)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.Index("IX_SystemApplications_Name")
                .OnTable("SystemApplications")
                .OnColumn("Name").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_SystemApplications_Audience")
                .OnTable("SystemApplications")
                .OnColumn("Audience").Ascending()
                .WithOptions().Unique();

            Create.Table("Contracts")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CompanyId").AsInt64().NotNullable()
                .WithColumn("SystemApplicationId").AsInt64().NotNullable()
                .WithColumn("StartDate").AsDateTimeOffset().NotNullable()
                .WithColumn("EndDate").AsDateTimeOffset().Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("ClientId").AsString(150).NotNullable()
                .WithColumn("ClientSecret").AsString(255).NotNullable()
                .WithColumn("AccessTokenLifetime").AsInt32().NotNullable()
                .WithColumn("RefreshTokenLifetime").AsInt32().NotNullable()
                .WithColumn("JwtSecretKey").AsString(255).NotNullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_Contracts_Companies_CompanyId")
                .FromTable("Contracts").ForeignColumn("CompanyId")
                .ToTable("Companies").PrimaryColumn("Id");

            Create.ForeignKey("FK_Contracts_SystemApplications_SystemApplicationId")
                .FromTable("Contracts").ForeignColumn("SystemApplicationId")
                .ToTable("SystemApplications").PrimaryColumn("Id");

            Create.Index("IX_Contracts_ClientId")
                .OnTable("Contracts")
                .OnColumn("ClientId").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_Contracts_CompanyId_SystemApplicationId")
                .OnTable("Contracts")
                .OnColumn("CompanyId").Ascending()
                .OnColumn("SystemApplicationId").Ascending()
                .WithOptions().Unique();

            Create.Table("Roles")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Name").AsString(120).NotNullable()
                .WithColumn("Description").AsString(500).NotNullable()
                .WithColumn("ContractId").AsInt64().NotNullable()
                .WithColumn("IsRoot").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("IsDefault").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_Roles_Contracts_ContractId")
                .FromTable("Roles").ForeignColumn("ContractId")
                .ToTable("Contracts").PrimaryColumn("Id");

            Create.Index("IX_Roles_ContractId_Name")
                .OnTable("Roles")
                .OnColumn("ContractId").Ascending()
                .OnColumn("Name").Ascending()
                .WithOptions().Unique();

            Create.Table("AccessResources")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Name").AsString(200).NotNullable()
                .WithColumn("Controller").AsString(120).NotNullable()
                .WithColumn("Action").AsString(120).NotNullable()
                .WithColumn("HttpMethod").AsString(20).NotNullable()
                .WithColumn("Route").AsString(500).NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.Index("IX_AccessResources_Name")
                .OnTable("AccessResources")
                .OnColumn("Name").Ascending()
                .WithOptions().Unique();

            Create.Table("UserRoles")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt64().NotNullable()
                .WithColumn("RoleId").AsInt64().NotNullable()
                .WithColumn("AssignedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("RevokedAt").AsDateTimeOffset().Nullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_UserRoles_Users_UserId")
                .FromTable("UserRoles").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id");

            Create.ForeignKey("FK_UserRoles_Roles_RoleId")
                .FromTable("UserRoles").ForeignColumn("RoleId")
                .ToTable("Roles").PrimaryColumn("Id");

            Create.Index("IX_UserRoles_UserId_RoleId")
                .OnTable("UserRoles")
                .OnColumn("UserId").Ascending()
                .OnColumn("RoleId").Ascending()
                .WithOptions().Unique();

            Create.Table("RoleAccessResources")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("RoleId").AsInt64().NotNullable()
                .WithColumn("AccessResourceId").AsInt64().NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_RoleAccessResources_Roles_RoleId")
                .FromTable("RoleAccessResources").ForeignColumn("RoleId")
                .ToTable("Roles").PrimaryColumn("Id");

            Create.ForeignKey("FK_RoleAccessResources_AccessResources_AccessResourceId")
                .FromTable("RoleAccessResources").ForeignColumn("AccessResourceId")
                .ToTable("AccessResources").PrimaryColumn("Id");

            Create.Index("IX_RoleAccessResources_RoleId_AccessResourceId")
                .OnTable("RoleAccessResources")
                .OnColumn("RoleId").Ascending()
                .OnColumn("AccessResourceId").Ascending()
                .WithOptions().Unique();

            Create.Table("LoginSessions")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("UserId").AsInt64().NotNullable()
                .WithColumn("ContractId").AsInt64().NotNullable()
                .WithColumn("SessionId").AsString(64).NotNullable()
                .WithColumn("IpAddress").AsString(45).Nullable()
                .WithColumn("UserAgent").AsString(1000).Nullable()
                .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("RevokedAt").AsDateTimeOffset().Nullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_LoginSessions_Users_UserId")
                .FromTable("LoginSessions").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id");

            Create.ForeignKey("FK_LoginSessions_Contracts_ContractId")
                .FromTable("LoginSessions").ForeignColumn("ContractId")
                .ToTable("Contracts").PrimaryColumn("Id");

            Create.Index("IX_LoginSessions_SessionId")
                .OnTable("LoginSessions")
                .OnColumn("SessionId").Ascending()
                .WithOptions().Unique();

            Create.Table("RefreshTokens")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Token").AsString(512).NotNullable()
                .WithColumn("UserId").AsInt64().NotNullable()
                .WithColumn("ContractId").AsInt64().Nullable()
                .WithColumn("SessionId").AsString(64).NotNullable()
                .WithColumn("Scopes").AsString(2000).NotNullable()
                .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
                .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("RevokedAt").AsDateTimeOffset().Nullable()
                .WithColumn("LastUsedAt").AsDateTimeOffset().Nullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_RefreshTokens_Users_UserId")
                .FromTable("RefreshTokens").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id");

            Create.ForeignKey("FK_RefreshTokens_Contracts_ContractId")
                .FromTable("RefreshTokens").ForeignColumn("ContractId")
                .ToTable("Contracts").PrimaryColumn("Id");

            Create.Index("IX_RefreshTokens_Token")
                .OnTable("RefreshTokens")
                .OnColumn("Token").Ascending()
                .WithOptions().Unique();

            Create.Table("AuthorizationCodes")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Code").AsString(512).NotNullable()
                .WithColumn("UserId").AsInt64().NotNullable()
                .WithColumn("ContractId").AsInt64().Nullable()
                .WithColumn("Scopes").AsString(2000).NotNullable()
                .WithColumn("RedirectUri").AsString(2000).NotNullable()
                .WithColumn("SessionId").AsString(64).NotNullable()
                .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
                .WithColumn("IsUsed").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("IsRevoked").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("Success").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("TokenExpiration").AsDateTimeOffset().Nullable()
                .WithColumn("UsedAt").AsDateTimeOffset().Nullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("UpdatedAt").AsDateTimeOffset().Nullable();

            Create.ForeignKey("FK_AuthorizationCodes_Users_UserId")
                .FromTable("AuthorizationCodes").ForeignColumn("UserId")
                .ToTable("Users").PrimaryColumn("Id");

            Create.ForeignKey("FK_AuthorizationCodes_Contracts_ContractId")
                .FromTable("AuthorizationCodes").ForeignColumn("ContractId")
                .ToTable("Contracts").PrimaryColumn("Id");

            Create.Index("IX_AuthorizationCodes_Code")
                .OnTable("AuthorizationCodes")
                .OnColumn("Code").Ascending()
                .WithOptions().Unique();
        }

        public override void Down()
        {
            Delete.Index("IX_AuthorizationCodes_Code").OnTable("AuthorizationCodes");
            Delete.ForeignKey("FK_AuthorizationCodes_Contracts_ContractId").OnTable("AuthorizationCodes");
            Delete.ForeignKey("FK_AuthorizationCodes_Users_UserId").OnTable("AuthorizationCodes");
            Delete.Table("AuthorizationCodes");

            Delete.Index("IX_RefreshTokens_Token").OnTable("RefreshTokens");
            Delete.ForeignKey("FK_RefreshTokens_Contracts_ContractId").OnTable("RefreshTokens");
            Delete.ForeignKey("FK_RefreshTokens_Users_UserId").OnTable("RefreshTokens");
            Delete.Table("RefreshTokens");

            Delete.Index("IX_LoginSessions_SessionId").OnTable("LoginSessions");
            Delete.ForeignKey("FK_LoginSessions_Contracts_ContractId").OnTable("LoginSessions");
            Delete.ForeignKey("FK_LoginSessions_Users_UserId").OnTable("LoginSessions");
            Delete.Table("LoginSessions");

            Delete.Index("IX_RoleAccessResources_RoleId_AccessResourceId").OnTable("RoleAccessResources");
            Delete.ForeignKey("FK_RoleAccessResources_AccessResources_AccessResourceId").OnTable("RoleAccessResources");
            Delete.ForeignKey("FK_RoleAccessResources_Roles_RoleId").OnTable("RoleAccessResources");
            Delete.Table("RoleAccessResources");

            Delete.Index("IX_UserRoles_UserId_RoleId").OnTable("UserRoles");
            Delete.ForeignKey("FK_UserRoles_Roles_RoleId").OnTable("UserRoles");
            Delete.ForeignKey("FK_UserRoles_Users_UserId").OnTable("UserRoles");
            Delete.Table("UserRoles");

            Delete.Index("IX_AccessResources_Name").OnTable("AccessResources");
            Delete.Table("AccessResources");

            Delete.Index("IX_Roles_ContractId_Name").OnTable("Roles");
            Delete.ForeignKey("FK_Roles_Contracts_ContractId").OnTable("Roles");
            Delete.Table("Roles");

            Delete.Index("IX_Contracts_CompanyId_SystemApplicationId").OnTable("Contracts");
            Delete.Index("IX_Contracts_ClientId").OnTable("Contracts");
            Delete.ForeignKey("FK_Contracts_SystemApplications_SystemApplicationId").OnTable("Contracts");
            Delete.ForeignKey("FK_Contracts_Companies_CompanyId").OnTable("Contracts");
            Delete.Table("Contracts");

            Delete.Index("IX_SystemApplications_Audience").OnTable("SystemApplications");
            Delete.Index("IX_SystemApplications_Name").OnTable("SystemApplications");
            Delete.Table("SystemApplications");

            Delete.Index("IX_Companies_Email").OnTable("Companies");
            Delete.Index("IX_Companies_Document").OnTable("Companies");
            Delete.Table("Companies");

            Delete.Index("IX_Users_Email").OnTable("Users");
            Delete.Index("IX_Users_Username").OnTable("Users");
            Delete.Table("Users");
        }
    }
}
