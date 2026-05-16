using FluentMigrator;

namespace IdentityManagement.Infrastructure.Migrations
{
    [Migration(202605160002)]
    public sealed class Migration_202605160002_PreventDuplicateUserRoleInContract : Migration
    {
        public override void Up()
        {
            Execute.Sql(@"
                CREATE OR REPLACE FUNCTION ensure_single_active_userrole_per_contract()
                RETURNS TRIGGER AS $$
                DECLARE
                    new_contractid BIGINT;
                BEGIN
                    IF NEW.isactive IS DISTINCT FROM TRUE OR NEW.revokedat IS NOT NULL THEN
                        RETURN NEW;
                    END IF;

                    SELECT contractid INTO new_contractid FROM roles WHERE id = NEW.roleid;

                    IF EXISTS (
                        SELECT 1
                        FROM userroles ur
                        JOIN roles r ON r.id = ur.roleid
                        WHERE ur.userid = NEW.userid
                          AND r.contractid = new_contractid
                          AND ur.isactive = TRUE
                          AND ur.revokedat IS NULL
                          AND ur.id IS DISTINCT FROM NEW.id
                    ) THEN
                        RAISE EXCEPTION 'usuario % ja possui role ativo no contrato %', NEW.userid, new_contractid
                            USING ERRCODE = '23505';
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_userroles_single_active_per_contract ON userroles;

                CREATE TRIGGER trg_userroles_single_active_per_contract
                BEFORE INSERT OR UPDATE ON userroles
                FOR EACH ROW
                EXECUTE FUNCTION ensure_single_active_userrole_per_contract();
            ");
        }

        public override void Down()
        {
            Execute.Sql(@"
                DROP TRIGGER IF EXISTS trg_userroles_single_active_per_contract ON userroles;
                DROP FUNCTION IF EXISTS ensure_single_active_userrole_per_contract();
            ");
        }
    }
}
