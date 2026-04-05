import { useEffect, useState } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, SearchableSelect, useApi, toast, useFormErrors } from 'archon-ui';
import { RoleService } from '../../services/roleService';
import { ContractService } from '../../services/contractService';
import { AccessResourceService } from '../../services/accessResourceService';
import type { Role, CreateRoleRequest, UpdateRoleRequest } from '../../types/role';
import type { Contract } from '../../types/contract';
import type { AccessResource } from '../../types/accessResource';
import RolePermissionsModal from './RolePermissionsModal';

interface RoleFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  role?: Role;
  contractId?: number;
  onSuccess: () => void;
}

export default function RoleFormModal({
  isOpen,
  onClose,
  role,
  contractId,
  onSuccess
}: RoleFormModalProps) {
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [contratos, setContratos] = useState<Contract[]>([]);
  const [accessResources, setAccessResources] = useState<AccessResource[]>([]);
  const [isPermissionsModalOpen, setIsPermissionsModalOpen] = useState(false);
  const [selectedResourceIds, setSelectedResourceIds] = useState<number[]>([]);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    contractId: 0,
    isRoot: false,
    isDefault: false,
  });

  const resetForm = () => {
    setSelectedResourceIds([]);
    setIsPermissionsModalOpen(false);
    setFormData({
      name: '',
      description: '',
      contractId: contractId || 0,
      isRoot: false,
      isDefault: false,
    });
    clearErrors();
  };

  const loadContratosApi = useApi({
    onSuccess: (data: Contract[]) => {
      setContratos(data);
    }
  });

  const savePerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: role ? 'Role atualizado com sucesso' : 'Role criado com sucesso',
      });
      resetForm();
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

  const loadAccessResourcesApi = useApi({
    onSuccess: (data: AccessResource[]) => {
      setAccessResources(data);
    },
    onError: (error) => {
      toast({
        title: 'Erro',
        description: error.message,
        variant: 'destructive',
      });
    }
  });

  useEffect(() => {
    if (isOpen) {
      loadContratosApi.execute(() => ContractService.getActive());
      loadAccessResourcesApi.execute(() => AccessResourceService.getAll());

      if (role) {
        setSelectedResourceIds(role.accessResourceIds ?? []);
        setFormData({
          name: role.name,
          description: role.description || '',
          contractId: role.contractId,
          isRoot: role.isRoot,
          isDefault: role.isDefault,
        });
      } else {
        setSelectedResourceIds([]);
        setFormData({
          name: '',
          description: '',
          contractId: contractId || 0,
          isRoot: false,
          isDefault: false,
        });
      }
    }
  }, [isOpen, role]);

  const selectedSystemApplicationId = contratos.find((contract) => contract.id === formData.contractId)?.systemApplicationId;

  const availableAccessResources = selectedSystemApplicationId
    ? accessResources.filter((resource) => resource.systemApplicationId === selectedSystemApplicationId)
    : [];

  useEffect(() => {
    if (!role && contratos.length > 0 && !contractId) {
      if (formData.contractId === 0) {
        setFormData(prev => ({ ...prev, contractId: contratos[0].id }));
      }
    }
  }, [contratos, role, contractId, formData.contractId]);

  const handleInputChange = (field: string, value: string | number | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (role) {
      const updateData: UpdateRoleRequest = {
        name: formData.name,
        description: formData.description,
        isRoot: formData.isRoot,
        isDefault: formData.isDefault,
        accessResourceIds: selectedResourceIds
      };
      await savePerfilApi.execute(() => RoleService.update(role.id, updateData));
    } else {
      const createData: CreateRoleRequest = {
        name: formData.name,
        description: formData.description,
        contractId: formData.contractId,
        isRoot: formData.isRoot,
        isDefault: formData.isDefault,
        accessResourceIds: selectedResourceIds
      };
      await savePerfilApi.execute(() => RoleService.create(createData));
    }
  };

  const handleClose = () => {
    resetForm();
    onClose();
  };

  const isValid = formData.name && formData.contractId;

  return (
    <>
      <Modal open={isOpen} onOpenChange={(open) => !open && handleClose()}>
        <ModalContent size="xl">
          <ModalHeader>
          <ModalTitle>{role ? 'Editar Role' : 'Novo Role'}</ModalTitle>
          </ModalHeader>

          <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Nome do Role <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.name}
              onChange={(e) => handleInputChange('name', e.target.value)}
              placeholder="Digite o nome do perfil"
              error={!!getError('name')}
              helperText={getError('name')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Descrição</label>
            <Input
              value={formData.description}
              onChange={(e) => handleInputChange('description', e.target.value)}
              placeholder="Digite uma descrição para o perfil"
              error={!!getError('description')}
              helperText={getError('description')}
            />
          </div>

          {!role && (
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Contract <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={contratos.map((contract) => ({
                  label: `${contract.companyName} - ${contract.systemApplicationName}`,
                  value: contract.id.toString()
                }))}
                value={formData.contractId.toString()}
                onValueChange={(value) => handleInputChange('contractId', parseInt(value))}
                placeholder="Selecione um contrato"
                searchPlaceholder="Pesquisar contrato..."
                disabled={!!contractId}
              />
            </div>
          )}

          <div className="flex gap-6 pt-2">
            <div className="flex items-center gap-2">
              <Switch
                checked={formData.isRoot}
                onCheckedChange={(checked) => handleInputChange('isRoot', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                Super Usuário
              </label>
            </div>
            <div className="flex items-center gap-2">
              <Switch
                checked={formData.isDefault}
                onCheckedChange={(checked) => handleInputChange('isDefault', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                Role Padrão
              </label>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Permissões</label>
            <div className="rounded-lg border bg-muted/20 p-4">
              <div className="flex items-start justify-between gap-4">
                <div className="space-y-1">
                  <p className="text-sm font-medium">Seleção por recurso</p>
                  <p className="text-sm text-muted-foreground">
                    Escolha as permissões em uma tela dedicada, agrupadas por recurso e endpoint.
                  </p>
                </div>
              </div>

              <div className="mt-4 flex flex-wrap items-center gap-3">
                <Button
                  variant="secondary"
                  onClick={() => setIsPermissionsModalOpen(true)}
                  disabled={formData.isRoot}
                >
                  Selecionar permissões
                </Button>

              </div>
            </div>
          </div>
          </div>

          <ModalFooter>
            <Button
              variant="outline"
              onClick={handleClose}
              disabled={savePerfilApi.isLoading}
            >
              Cancelar
            </Button>
            <Button
              variant="primary"
              onClick={handleSave}
              loading={savePerfilApi.isLoading}
              disabled={!isValid}
            >
              {role ? 'Atualizar' : 'Criar'}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>

      <RolePermissionsModal
        isOpen={isPermissionsModalOpen}
        onClose={() => setIsPermissionsModalOpen(false)}
        resources={availableAccessResources}
        selectedResourceIds={selectedResourceIds}
        onConfirm={setSelectedResourceIds}
        disabled={formData.isRoot}
      />
    </>
  );
}
