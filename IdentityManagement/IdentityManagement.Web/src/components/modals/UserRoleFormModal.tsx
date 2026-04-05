import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, SearchableSelect, useApi, toast, useFormErrors, useI18n } from 'archon-ui';
import { UserRoleService } from '../../services/userRoleService';
import { UserService } from '../../services/userService';
import { RoleService } from '../../services/roleService';
import type { AssignUserToRoleRequest } from '../../types/userRole';
import type { User } from '../../types/user';
import type { Role } from '../../types/role';

interface UserRoleFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  contractId: number;
  onSuccess: () => void;
}

export default function UserRoleFormModal({
  isOpen,
  onClose,
  contractId,
  onSuccess
}: UserRoleFormModalProps) {
  const { t } = useI18n()
  const { setErrors, clearErrors } = useFormErrors();
  const [usuarios, setUsuarios] = useState<User[]>([]);
  const [perfis, setPerfis] = useState<Role[]>([]);
  const [formData, setFormData] = useState({
    userId: 0,
    roleId: 0
  });

  const loadUsuariosApi = useApi({
    onSuccess: (data: any) => {
      setUsuarios(data.data || data);
    }
  });

  const loadPerfisApi = useApi({
    onSuccess: (data: Role[]) => {
      setPerfis(data);
    }
  });

  const saveUsuarioPerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('userRole.form.toast.created'),
      });
      clearErrors();
      onSuccess();
      onClose();
    },
    onError: (error) => {
      setErrors(error);
      if (!error.errors) {
        toast({
          title: t('common.toast.errorTitle'),
          description: error.message,
          variant: 'destructive',
        });
      }
    }
  });

  useEffect(() => {
    if (isOpen && contractId) {
      loadUsuariosApi.execute(() => UserService.getAll({ page: 1, pageSize: 1000 }));
      loadPerfisApi.execute(async () => {
        const data = await RoleService.getByContract(contractId, { page: 1, pageSize: 1000 });
        return data.data;
      });

      setFormData({
        userId: 0,
        roleId: 0
      });
    }
  }, [isOpen, contractId]);

  const handleInputChange = (field: string, value: number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    const createData: AssignUserToRoleRequest = {
      userId: formData.userId,
      roleId: formData.roleId
    };
    await saveUsuarioPerfilApi.execute(() => UserRoleService.assign(createData));
  };

  const isValid = formData.userId && formData.roleId;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{t('userRole.form.title')}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('userRole.field.user')} <span className="text-destructive">*</span>
            </label>
            <SearchableSelect
              options={usuarios.map(usuario => ({
                value: usuario.id.toString(),
                label: `${usuario.name} (${usuario.email})`
              }))}
              value={formData.userId.toString()}
              onValueChange={(value) => handleInputChange('userId', parseInt(value))}
              placeholder={t('userRole.form.userPlaceholder')}
              searchPlaceholder={t('userRole.form.userSearchPlaceholder')}
              disabled={loadUsuariosApi.isLoading}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {t('userRole.field.role')} <span className="text-destructive">*</span>
            </label>
            <SearchableSelect
              options={perfis.map(perfil => ({
                value: perfil.id.toString(),
                label: perfil.name
              }))}
              value={formData.roleId.toString()}
              onValueChange={(value) => handleInputChange('roleId', parseInt(value))}
              placeholder={t('userRole.form.rolePlaceholder')}
              searchPlaceholder={t('userRole.form.roleSearchPlaceholder')}
              disabled={loadPerfisApi.isLoading}
            />
          </div>
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
            disabled={saveUsuarioPerfilApi.isLoading}
          >
            {t('common.action.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveUsuarioPerfilApi.isLoading}
            disabled={!isValid}
          >
            {t('common.action.link')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
