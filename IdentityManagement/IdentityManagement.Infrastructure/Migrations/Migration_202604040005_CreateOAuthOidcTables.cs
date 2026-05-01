using FluentMigrator;
using System.Security.Cryptography;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040005)]
    public sealed class Migration_202604040005_CreateOAuthOidcTables : Migration
    {
        public override void Up()
        {
            Alter.Table("authorizationcodes")
                .AddColumn("clientid").AsString(120).NotNullable().WithDefaultValue(string.Empty)
                .AddColumn("nonce").AsString(512).NotNullable().WithDefaultValue(string.Empty)
                .AddColumn("codechallenge").AsString(256).NotNullable().WithDefaultValue(string.Empty)
                .AddColumn("codechallengemethod").AsString(20).NotNullable().WithDefaultValue(string.Empty);

            Alter.Table("refreshtokens")
                .AddColumn("clientid").AsString(120).NotNullable().WithDefaultValue(string.Empty);

            Create.Table("oauthclients")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("systemapplicationid").AsInt64().NotNullable()
                .WithColumn("clientid").AsString(120).NotNullable()
                .WithColumn("clientname").AsString(150).NotNullable()
                .WithColumn("clienttype").AsInt32().NotNullable()
                .WithColumn("clientsecrethash").AsString(512).NotNullable().WithDefaultValue(string.Empty)
                .WithColumn("requirepkce").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("requireconsent").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("allowofflineaccess").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("accesstokenlifetime").AsInt32().NotNullable().WithDefaultValue(900)
                .WithColumn("identitytokenlifetime").AsInt32().NotNullable().WithDefaultValue(900)
                .WithColumn("refreshtokenlifetime").AsInt32().NotNullable().WithDefaultValue(2592000)
                .WithColumn("refreshtokenrotationenabled").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_oauthclients_systemapplications_systemapplicationid")
                .FromTable("oauthclients").ForeignColumn("systemapplicationid")
                .ToTable("systemapplications").PrimaryColumn("id");

            Create.Index("ix_oauthclients_clientid")
                .OnTable("oauthclients")
                .OnColumn("clientid").Ascending()
                .WithOptions().Unique();

            Create.Table("oauthclientredirecturis")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("oauthclientid").AsInt64().NotNullable()
                .WithColumn("uri").AsString(2000).NotNullable()
                .WithColumn("type").AsInt32().NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_oauthclientredirecturis_oauthclients_oauthclientid")
                .FromTable("oauthclientredirecturis").ForeignColumn("oauthclientid")
                .ToTable("oauthclients").PrimaryColumn("id");

            Create.Index("ix_oauthclientredirecturis_clientid_uri_type")
                .OnTable("oauthclientredirecturis")
                .OnColumn("oauthclientid").Ascending()
                .OnColumn("uri").Ascending()
                .OnColumn("type").Ascending()
                .WithOptions().Unique();

            Create.Table("oauthscopes")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("name").AsString(120).NotNullable()
                .WithColumn("displayname").AsString(150).NotNullable()
                .WithColumn("description").AsString(500).NotNullable()
                .WithColumn("isidentityscope").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("isapiscope").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ix_oauthscopes_name")
                .OnTable("oauthscopes")
                .OnColumn("name").Ascending()
                .WithOptions().Unique();

            Insert.IntoTable("oauthscopes").Row(new
            {
                name = "openid",
                displayname = "OpenID",
                description = "OpenID Connect identity scope.",
                isidentityscope = true,
                isapiscope = false,
                isactive = true,
                createdat = DateTimeOffset.UtcNow
            });

            Insert.IntoTable("oauthscopes").Row(new
            {
                name = "profile",
                displayname = "Profile",
                description = "Basic user profile claims.",
                isidentityscope = true,
                isapiscope = false,
                isactive = true,
                createdat = DateTimeOffset.UtcNow
            });

            Insert.IntoTable("oauthscopes").Row(new
            {
                name = "email",
                displayname = "Email",
                description = "User email claims.",
                isidentityscope = true,
                isapiscope = false,
                isactive = true,
                createdat = DateTimeOffset.UtcNow
            });

            Insert.IntoTable("oauthscopes").Row(new
            {
                name = "offline_access",
                displayname = "Offline Access",
                description = "Allows refresh token issuance.",
                isidentityscope = false,
                isapiscope = false,
                isactive = true,
                createdat = DateTimeOffset.UtcNow
            });

            Create.Table("oauthclientscopes")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("oauthclientid").AsInt64().NotNullable()
                .WithColumn("oauthscopeid").AsInt64().NotNullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.ForeignKey("fk_oauthclientscopes_oauthclients_oauthclientid")
                .FromTable("oauthclientscopes").ForeignColumn("oauthclientid")
                .ToTable("oauthclients").PrimaryColumn("id");

            Create.ForeignKey("fk_oauthclientscopes_oauthscopes_oauthscopeid")
                .FromTable("oauthclientscopes").ForeignColumn("oauthscopeid")
                .ToTable("oauthscopes").PrimaryColumn("id");

            Create.Index("ix_oauthclientscopes_clientid_scopeid")
                .OnTable("oauthclientscopes")
                .OnColumn("oauthclientid").Ascending()
                .OnColumn("oauthscopeid").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                INSERT INTO oauthclients (
                    systemapplicationid,
                    clientid,
                    clientname,
                    clienttype,
                    clientsecrethash,
                    requirepkce,
                    requireconsent,
                    allowofflineaccess,
                    isactive,
                    accesstokenlifetime,
                    identitytokenlifetime,
                    refreshtokenlifetime,
                    refreshtokenrotationenabled,
                    createdat
                )
                SELECT
                    systemapplications.id,
                    'identity-management-web',
                    'Identity Management Web',
                    1,
                    '',
                    true,
                    false,
                    true,
                    true,
                    900,
                    900,
                    2592000,
                    true,
                    NOW()
                FROM systemapplications
                WHERE systemapplications.audience = 'identity-management'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM oauthclients
                      WHERE oauthclients.clientid = 'identity-management-web'
                  );

                INSERT INTO oauthclientredirecturis (oauthclientid, uri, type, isactive, createdat)
                SELECT oauthclients.id, redirecturis.uri, redirecturis.type, true, NOW()
                FROM oauthclients
                CROSS JOIN (
                    VALUES
                        ('http://localhost:5173/callback', 1),
                        ('https://localhost:7078/callback', 1),
                        ('http://localhost:5173/', 2),
                        ('https://localhost:7078/', 2)
                ) AS redirecturis(uri, type)
                WHERE oauthclients.clientid = 'identity-management-web'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM oauthclientredirecturis
                      WHERE oauthclientredirecturis.oauthclientid = oauthclients.id
                        AND oauthclientredirecturis.uri = redirecturis.uri
                        AND oauthclientredirecturis.type = redirecturis.type
                  );

                INSERT INTO oauthclientscopes (oauthclientid, oauthscopeid, isactive, createdat)
                SELECT oauthclients.id, oauthscopes.id, true, NOW()
                FROM oauthclients
                JOIN oauthscopes ON oauthscopes.name IN ('openid', 'profile', 'email', 'offline_access')
                WHERE oauthclients.clientid = 'identity-management-web'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM oauthclientscopes
                      WHERE oauthclientscopes.oauthclientid = oauthclients.id
                        AND oauthclientscopes.oauthscopeid = oauthscopes.id
                  );
                """);

            Create.Table("signingkeys")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("keyid").AsString(120).NotNullable()
                .WithColumn("algorithm").AsString(30).NotNullable()
                .WithColumn("publickeypem").AsString(4000).NotNullable()
                .WithColumn("privatekeyencrypted").AsString(8000).NotNullable().WithDefaultValue(string.Empty)
                .WithColumn("notbefore").AsDateTimeOffset().NotNullable()
                .WithColumn("expiresat").AsDateTimeOffset().Nullable()
                .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("revokedat").AsDateTimeOffset().Nullable()
                .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                .WithColumn("updatedat").AsDateTimeOffset().Nullable();

            Create.Index("ix_signingkeys_keyid")
                .OnTable("signingkeys")
                .OnColumn("keyid").Ascending()
                .WithOptions().Unique();

            using RSA rsa = RSA.Create(2048);
            Insert.IntoTable("signingkeys").Row(new
            {
                keyid = $"oidc-{Guid.NewGuid():N}",
                algorithm = "RS256",
                publickeypem = rsa.ExportSubjectPublicKeyInfoPem(),
                privatekeyencrypted = rsa.ExportPkcs8PrivateKeyPem(),
                notbefore = DateTimeOffset.UtcNow,
                expiresat = DateTimeOffset.UtcNow.AddYears(2),
                isactive = true,
                createdat = DateTimeOffset.UtcNow
            });
        }

        public override void Down()
        {
            Delete.Column("clientid").FromTable("refreshtokens");

            Delete.Column("codechallengemethod").FromTable("authorizationcodes");
            Delete.Column("codechallenge").FromTable("authorizationcodes");
            Delete.Column("nonce").FromTable("authorizationcodes");
            Delete.Column("clientid").FromTable("authorizationcodes");

            Delete.Index("ix_signingkeys_keyid").OnTable("signingkeys");
            Delete.Table("signingkeys");

            Delete.Index("ix_oauthclientscopes_clientid_scopeid").OnTable("oauthclientscopes");
            Delete.ForeignKey("fk_oauthclientscopes_oauthscopes_oauthscopeid").OnTable("oauthclientscopes");
            Delete.ForeignKey("fk_oauthclientscopes_oauthclients_oauthclientid").OnTable("oauthclientscopes");
            Delete.Table("oauthclientscopes");

            Delete.Index("ix_oauthscopes_name").OnTable("oauthscopes");
            Delete.Table("oauthscopes");

            Delete.Index("ix_oauthclientredirecturis_clientid_uri_type").OnTable("oauthclientredirecturis");
            Delete.ForeignKey("fk_oauthclientredirecturis_oauthclients_oauthclientid").OnTable("oauthclientredirecturis");
            Delete.Table("oauthclientredirecturis");

            Delete.Index("ix_oauthclients_clientid").OnTable("oauthclients");
            Delete.ForeignKey("fk_oauthclients_systemapplications_systemapplicationid").OnTable("oauthclients");
            Delete.Table("oauthclients");
        }
    }
}
