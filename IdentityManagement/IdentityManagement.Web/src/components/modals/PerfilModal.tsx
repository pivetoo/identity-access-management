import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, SearchableSelect, useApi, toast, useFormErrors } from 'archon-ui';
import { PerfilService } from '../../services/perfilService';
import { ContratoService } from '../../services/contratoService';
import type { Perfil, CreatePerfilRequest, UpdatePerfilRequest } from '../../types/perfil';
import type { Contrato } from '../../types/contrato';

interface PerfilModalProps {
  isOpen: boolean;
  onClose: () => void;
  perfil?: Perfil;
  contratoId?: number;
  onSuccess: () => void;
}

export default function PerfilModal({
  isOpen,
  onClose,
  perfil,
  contratoId,
  onSuccess
}: PerfilModalProps) {
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [contratos, setContratos] = useState<Contrato[]>([]);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    contratoId: 0,
    isSuperUser: false,
    isDefault: false,
    permissions: ''
  });

  const loadContratosApi = useApi({
    onSuccess: (data: Contrato[]) => {
      setContratos(data);
    }
  });

  const savePerfilApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: perfil ? 'Perfil atualizado com sucesso' : 'Perfil criado com sucesso',
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
    if (isOpen) {
      loadContratosApi.execute(() => ContratoService.getActive());

      if (perfil) {
        setFormData({
          name: perfil.name,
          description: perfil.description || '',
          contratoId: perfil.contratoId,
          isSuperUser: perfil.isSuperUser,
          isDefault: perfil.isDefault,
          permissions: perfil.permissions || ''
        });
      } else {
        setFormData({
          name: '',
          description: '',
          contratoId: contratoId || 0,
          isSuperUser: false,
          isDefault: false,
          permissions: ''
        });
      }
    }
  }, [isOpen, perfil]);

  useEffect(() => {
    if (!perfil && contratos.length > 0 && !contratoId) {
      if (formData.contratoId === 0) {
        setFormData(prev => ({ ...prev, contratoId: contratos[0].id }));
      }
    }
  }, [contratos, perfil, contratoId, formData.contratoId]);

  const handleInputChange = (field: string, value: string | number | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (perfil) {
      const updateData: UpdatePerfilRequest = {
        name: formData.name,
        description: formData.description,
        isSuperUser: formData.isSuperUser,
        isDefault: formData.isDefault,
        permissions: formData.permissions
      };
      await savePerfilApi.execute(() => PerfilService.update(perfil.id, updateData));
    } else {
      const createData: CreatePerfilRequest = {
        name: formData.name,
        description: formData.description,
        contratoId: formData.contratoId,
        isSuperUser: formData.isSuperUser,
        isDefault: formData.isDefault,
        permissions: formData.permissions
      };
      await savePerfilApi.execute(() => PerfilService.create(createData));
    }
  };

  const isValid = formData.name && formData.contratoId;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{perfil ? 'Editar Perfil' : 'Novo Perfil'}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Nome do Perfil <span className="text-destructive">*</span>
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

          {!perfil && (
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Contrato <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={contratos.map((contrato) => ({
                  label: `${contrato.empresaName} - ${contrato.sistemaName}`,
                  value: contrato.id.toString()
                }))}
                value={formData.contratoId.toString()}
                onValueChange={(value) => handleInputChange('contratoId', parseInt(value))}
                placeholder="Selecione um contrato"
                searchPlaceholder="Pesquisar contrato..."
                disabled={!!contratoId}
              />
            </div>
          )}

          <div className="flex gap-6 pt-2">
            <div className="flex items-center gap-2">
              <Switch
                checked={formData.isSuperUser}
                onCheckedChange={(checked) => handleInputChange('isSuperUser', checked)}
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
                Perfil Padrão
              </label>
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Permissões</label>
            <textarea
              value={formData.permissions}
              onChange={(e) => handleInputChange('permissions', e.target.value)}
              placeholder="Digite as permissões separadas por virgula (ex: user.read, user.write, admin.access)"
              rows={4}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 resize-none"
            />
            <p className="text-xs text-muted-foreground">
              {formData.isSuperUser
                ? 'Super usuário tem acesso a todas as permissões automaticamente'
                : 'Separe as permissões por virgula'}
            </p>
          </div>
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
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
            {perfil ? 'Atualizar' : 'Criar'}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
