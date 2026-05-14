import { useEffect, useState } from 'react';
import { Pencil, Plus, Shield, ShieldCheck, Star } from 'lucide-react';
import { Badge, Button, Card, CardContent, useApi, useI18n } from 'archon-ui';
import { RoleService } from '../../../services/roleService';
import type { Role } from '../../../types/role';
import RoleFormModal from '../../../components/modals/RoleFormModal';

interface ContractRolesTabProps {
  contractId: number;
  refreshKey: number;
  onRefresh: () => void;
}

export default function ContractRolesTab({ contractId, refreshKey, onRefresh }: ContractRolesTabProps) {
  const { t } = useI18n();
  const [roles, setRoles] = useState<Role[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<Role | undefined>();

  const loadApi = useApi({
    onSuccess: (data: Role[]) => setRoles(data),
  });

  useEffect(() => {
    loadApi.execute(() => RoleService.getByContractSummary(contractId));
  }, [contractId, refreshKey]);

  const handleAdd = () => {
    setEditingRole(undefined);
    setIsModalOpen(true);
  };

  const handleEdit = (role: Role) => {
    setEditingRole(role);
    setIsModalOpen(true);
  };

  if (loadApi.isLoading && roles.length === 0) {
    return <div className="h-32 animate-pulse rounded-lg border border-border bg-muted/30" />;
  }

  return (
    <>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-sm font-semibold text-foreground">
              {t('contractDetail.roles.heading')}
            </h3>
            <p className="text-xs text-muted-foreground">
              {t('contractDetail.roles.subheading').replace('{0}', roles.length.toString())}
            </p>
          </div>
          <Button variant="primary" size="sm" onClick={handleAdd}>
            <Plus className="mr-2 h-4 w-4" />
            {t('contractDetail.roles.addRole')}
          </Button>
        </div>

        {roles.length === 0 ? (
          <Card>
            <CardContent className="py-10 text-center">
              <h3 className="text-sm font-semibold text-foreground">
                {t('contractDetail.roles.empty.title')}
              </h3>
              <p className="mt-1 text-xs text-muted-foreground">
                {t('contractDetail.roles.empty.description')}
              </p>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {roles.map((role) => (
              <Card key={role.id}>
                <CardContent className="space-y-3 p-4">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex items-center gap-2 min-w-0">
                      <div className="rounded-md bg-primary/15 p-1.5 text-primary">
                        {role.isRoot ? <ShieldCheck className="h-4 w-4" /> : <Shield className="h-4 w-4" />}
                      </div>
                      <div className="min-w-0">
                        <div className="truncate text-sm font-semibold text-foreground">
                          {role.name}
                        </div>
                        {role.description && (
                          <div className="truncate text-xs text-muted-foreground">
                            {role.description}
                          </div>
                        )}
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => handleEdit(role)}
                      className="text-muted-foreground hover:text-foreground"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                  </div>

                  <div className="flex flex-wrap gap-1">
                    {role.isRoot && (
                      <Badge variant="info" className="text-[10px]">
                        {t('contractDetail.roles.badge.root')}
                      </Badge>
                    )}
                    {role.isDefault && (
                      <Badge variant="secondary" className="text-[10px]">
                        <Star className="mr-1 h-2.5 w-2.5" />
                        {t('contractDetail.roles.badge.default')}
                      </Badge>
                    )}
                    {role.accessResourceIds && role.accessResourceIds.length > 0 && (
                      <Badge variant="outline" className="text-[10px]">
                        {t('contractDetail.roles.badge.permissions').replace(
                          '{0}',
                          role.accessResourceIds.length.toString(),
                        )}
                      </Badge>
                    )}
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      <RoleFormModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        role={editingRole}
        contractId={contractId}
        onSuccess={() => {
          setIsModalOpen(false);
          onRefresh();
        }}
      />
    </>
  );
}
