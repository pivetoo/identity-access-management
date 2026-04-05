import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Button, SearchableSelect, useApi, toast, useFormErrors } from 'archon-ui';
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
        title: 'Sucesso',
        description: 'Usuário vinculado ao perfil com sucesso',
      });
      clearErrors();
      onSuccess();
      onClose();
    },
    onError: (error) => {
      setErrors(error);
      if (!error.errors) {
        toast({
          title: 'Erro',
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
          <ModalTitle>Vincular Usuário ao Role</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Usuário <span className="text-destructive">*</span>
            </label>
            <SearchableSelect
              options={usuarios.map(usuario => ({
                value: usuario.id.toString(),
                label: `${usuario.name} (${usuario.email})`
              }))}
              value={formData.userId.toString()}
              onValueChange={(value) => handleInputChange('userId', parseInt(value))}
              placeholder="Selecione um usuário"
              searchPlaceholder="Pesquisar usuário..."
              disabled={loadUsuariosApi.isLoading}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Role <span className="text-destructive">*</span>
            </label>
            <SearchableSelect
              options={perfis.map(perfil => ({
                value: perfil.id.toString(),
                label: perfil.name
              }))}
              value={formData.roleId.toString()}
              onValueChange={(value) => handleInputChange('roleId', parseInt(value))}
              placeholder="Selecione um perfil"
              searchPlaceholder="Pesquisar perfil..."
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
            Cancelar
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveUsuarioPerfilApi.isLoading}
            disabled={!isValid}
          >
            Vincular
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
