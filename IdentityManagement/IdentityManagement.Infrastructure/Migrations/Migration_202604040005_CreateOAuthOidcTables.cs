using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202604040005)]
    public sealed class Migration_202604040005_CreateOAuthOidcTables : Migration
    {
        public override void Up()
        {
            AddAuthorizationCodeColumns();
            AddRefreshTokenColumns();
            CreateOAuthClientsTable();
            CreateOAuthClientRedirectUrisTable();
            CreateOAuthScopesTable();
            CreateOAuthClientScopesTable();
            CreateSigningKeysTable();
        }

        public override void Down()
        {
            RemoveAuthorizationCodeColumns();
            RemoveRefreshTokenColumns();
            DeleteSigningKeysTable();
            DeleteOAuthClientScopesTable();
            DeleteOAuthScopesTable();
            DeleteOAuthClientRedirectUrisTable();
            DeleteOAuthClientsTable();
        }

        private void AddAuthorizationCodeColumns()
        {
            if (!Schema.Table("authorizationcodes").Column("clientid").Exists())
            {
                Alter.Table("authorizationcodes")
                    .AddColumn("clientid").AsString(120).NotNullable().WithDefaultValue(string.Empty);
            }

            if (!Schema.Table("authorizationcodes").Column("nonce").Exists())
            {
                Alter.Table("authorizationcodes")
                    .AddColumn("nonce").AsString(512).NotNullable().WithDefaultValue(string.Empty);
            }

            if (!Schema.Table("authorizationcodes").Column("codechallenge").Exists())
            {
                Alter.Table("authorizationcodes")
                    .AddColumn("codechallenge").AsString(256).NotNullable().WithDefaultValue(string.Empty);
            }

            if (!Schema.Table("authorizationcodes").Column("codechallengemethod").Exists())
            {
                Alter.Table("authorizationcodes")
                    .AddColumn("codechallengemethod").AsString(20).NotNullable().WithDefaultValue(string.Empty);
            }
        }

        private void AddRefreshTokenColumns()
        {
            if (!Schema.Table("refreshtokens").Column("clientid").Exists())
            {
                Alter.Table("refreshtokens")
                    .AddColumn("clientid").AsString(120).NotNullable().WithDefaultValue(string.Empty);
            }
        }

        private void CreateOAuthClientsTable()
        {
            if (!Schema.Table("oauthclients").Exists())
            {
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
            }

            if (!Schema.Table("oauthclients").Constraint("fk_oauthclients_systemapplications_systemapplicationid").Exists())
            {
                Create.ForeignKey("fk_oauthclients_systemapplications_systemapplicationid")
                    .FromTable("oauthclients").ForeignColumn("systemapplicationid")
                    .ToTable("systemapplications").PrimaryColumn("id");
            }

            if (!Schema.Table("oauthclients").Index("ix_oauthclients_clientid").Exists())
            {
                Create.Index("ix_oauthclients_clientid")
                    .OnTable("oauthclients")
                    .OnColumn("clientid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateOAuthClientRedirectUrisTable()
        {
            if (!Schema.Table("oauthclientredirecturis").Exists())
            {
                Create.Table("oauthclientredirecturis")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("oauthclientid").AsInt64().NotNullable()
                    .WithColumn("uri").AsString(2000).NotNullable()
                    .WithColumn("type").AsInt32().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("oauthclientredirecturis").Constraint("fk_oauthclientredirecturis_oauthclients_oauthclientid").Exists())
            {
                Create.ForeignKey("fk_oauthclientredirecturis_oauthclients_oauthclientid")
                    .FromTable("oauthclientredirecturis").ForeignColumn("oauthclientid")
                    .ToTable("oauthclients").PrimaryColumn("id");
            }

            if (!Schema.Table("oauthclientredirecturis").Index("ix_oauthclientredirecturis_clientid_uri_type").Exists())
            {
                Create.Index("ix_oauthclientredirecturis_clientid_uri_type")
                    .OnTable("oauthclientredirecturis")
                    .OnColumn("oauthclientid").Ascending()
                    .OnColumn("uri").Ascending()
                    .OnColumn("type").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateOAuthScopesTable()
        {
            if (!Schema.Table("oauthscopes").Exists())
            {
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
            }

            if (!Schema.Table("oauthscopes").Index("ix_oauthscopes_name").Exists())
            {
                Create.Index("ix_oauthscopes_name")
                    .OnTable("oauthscopes")
                    .OnColumn("name").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateOAuthClientScopesTable()
        {
            if (!Schema.Table("oauthclientscopes").Exists())
            {
                Create.Table("oauthclientscopes")
                    .WithColumn("id").AsInt64().PrimaryKey().Identity()
                    .WithColumn("oauthclientid").AsInt64().NotNullable()
                    .WithColumn("oauthscopeid").AsInt64().NotNullable()
                    .WithColumn("isactive").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("createdat").AsDateTimeOffset().NotNullable()
                    .WithColumn("updatedat").AsDateTimeOffset().Nullable();
            }

            if (!Schema.Table("oauthclientscopes").Constraint("fk_oauthclientscopes_oauthclients_oauthclientid").Exists())
            {
                Create.ForeignKey("fk_oauthclientscopes_oauthclients_oauthclientid")
                    .FromTable("oauthclientscopes").ForeignColumn("oauthclientid")
                    .ToTable("oauthclients").PrimaryColumn("id");
            }

            if (!Schema.Table("oauthclientscopes").Constraint("fk_oauthclientscopes_oauthscopes_oauthscopeid").Exists())
            {
                Create.ForeignKey("fk_oauthclientscopes_oauthscopes_oauthscopeid")
                    .FromTable("oauthclientscopes").ForeignColumn("oauthscopeid")
                    .ToTable("oauthscopes").PrimaryColumn("id");
            }

            if (!Schema.Table("oauthclientscopes").Index("ix_oauthclientscopes_clientid_scopeid").Exists())
            {
                Create.Index("ix_oauthclientscopes_clientid_scopeid")
                    .OnTable("oauthclientscopes")
                    .OnColumn("oauthclientid").Ascending()
                    .OnColumn("oauthscopeid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void CreateSigningKeysTable()
        {
            if (!Schema.Table("signingkeys").Exists())
            {
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
            }

            if (!Schema.Table("signingkeys").Index("ix_signingkeys_keyid").Exists())
            {
                Create.Index("ix_signingkeys_keyid")
                    .OnTable("signingkeys")
                    .OnColumn("keyid").Ascending()
                    .WithOptions().Unique();
            }
        }

        private void RemoveAuthorizationCodeColumns()
        {
            if (!Schema.Table("authorizationcodes").Exists())
            {
                return;
            }

            if (Schema.Table("authorizationcodes").Column("codechallengemethod").Exists())
            {
                Delete.Column("codechallengemethod").FromTable("authorizationcodes");
            }

            if (Schema.Table("authorizationcodes").Column("codechallenge").Exists())
            {
                Delete.Column("codechallenge").FromTable("authorizationcodes");
            }

            if (Schema.Table("authorizationcodes").Column("nonce").Exists())
            {
                Delete.Column("nonce").FromTable("authorizationcodes");
            }

            if (Schema.Table("authorizationcodes").Column("clientid").Exists())
            {
                Delete.Column("clientid").FromTable("authorizationcodes");
            }
        }

        private void RemoveRefreshTokenColumns()
        {
            if (Schema.Table("refreshtokens").Column("clientid").Exists())
            {
                Delete.Column("clientid").FromTable("refreshtokens");
            }
        }

        private void DeleteSigningKeysTable()
        {
            if (!Schema.Table("signingkeys").Exists())
            {
                return;
            }

            if (Schema.Table("signingkeys").Index("ix_signingkeys_keyid").Exists())
            {
                Delete.Index("ix_signingkeys_keyid").OnTable("signingkeys");
            }

            Delete.Table("signingkeys");
        }

        private void DeleteOAuthClientScopesTable()
        {
            if (!Schema.Table("oauthclientscopes").Exists())
            {
                return;
            }

            if (Schema.Table("oauthclientscopes").Index("ix_oauthclientscopes_clientid_scopeid").Exists())
            {
                Delete.Index("ix_oauthclientscopes_clientid_scopeid").OnTable("oauthclientscopes");
            }

            if (Schema.Table("oauthclientscopes").Constraint("fk_oauthclientscopes_oauthscopes_oauthscopeid").Exists())
            {
                Delete.ForeignKey("fk_oauthclientscopes_oauthscopes_oauthscopeid").OnTable("oauthclientscopes");
            }

            if (Schema.Table("oauthclientscopes").Constraint("fk_oauthclientscopes_oauthclients_oauthclientid").Exists())
            {
                Delete.ForeignKey("fk_oauthclientscopes_oauthclients_oauthclientid").OnTable("oauthclientscopes");
            }

            Delete.Table("oauthclientscopes");
        }

        private void DeleteOAuthScopesTable()
        {
            if (!Schema.Table("oauthscopes").Exists())
            {
                return;
            }

            if (Schema.Table("oauthscopes").Index("ix_oauthscopes_name").Exists())
            {
                Delete.Index("ix_oauthscopes_name").OnTable("oauthscopes");
            }

            Delete.Table("oauthscopes");
        }

        private void DeleteOAuthClientRedirectUrisTable()
        {
            if (!Schema.Table("oauthclientredirecturis").Exists())
            {
                return;
            }

            if (Schema.Table("oauthclientredirecturis").Index("ix_oauthclientredirecturis_clientid_uri_type").Exists())
            {
                Delete.Index("ix_oauthclientredirecturis_clientid_uri_type").OnTable("oauthclientredirecturis");
            }

            if (Schema.Table("oauthclientredirecturis").Constraint("fk_oauthclientredirecturis_oauthclients_oauthclientid").Exists())
            {
                Delete.ForeignKey("fk_oauthclientredirecturis_oauthclients_oauthclientid").OnTable("oauthclientredirecturis");
            }

            Delete.Table("oauthclientredirecturis");
        }

        private void DeleteOAuthClientsTable()
        {
            if (!Schema.Table("oauthclients").Exists())
            {
                return;
            }

            if (Schema.Table("oauthclients").Index("ix_oauthclients_clientid").Exists())
            {
                Delete.Index("ix_oauthclients_clientid").OnTable("oauthclients");
            }

            if (Schema.Table("oauthclients").Constraint("fk_oauthclients_systemapplications_systemapplicationid").Exists())
            {
                Delete.ForeignKey("fk_oauthclients_systemapplications_systemapplicationid").OnTable("oauthclients");
            }

            Delete.Table("oauthclients");
        }
    }
}
