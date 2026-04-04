import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, useApi, toast, useFormErrors } from 'archon-ui';
import { SistemaService } from '../../services/sistemaService';
import type { Sistema, CreateSistemaRequest, UpdateSistemaRequest } from '../../types/sistema';

interface SistemaModalProps {
  isOpen: boolean;
  onClose: () => void;
  sistema?: Sistema;
  onSuccess: () => void;
}

export default function SistemaModal({
  isOpen,
  onClose,
  sistema,
  onSuccess
}: SistemaModalProps) {
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    redirectUris: '',
    isActive: true,
    audience: ''
  });

  const saveSistemaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: sistema ? 'Sistema atualizado com sucesso' : 'Sistema criado com sucesso',
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
      if (sistema) {
        setFormData({
          name: sistema.name,
          description: sistema.description || '',
          redirectUris: sistema.redirectUris,
          isActive: sistema.isActive,
          audience: sistema.audience
        });
      } else {
        setFormData({
          name: '',
          description: '',
          redirectUris: '',
          isActive: true,
          audience: ''
        });
      }
    }
  }, [isOpen, sistema]);

  const handleInputChange = (field: string, value: string | boolean | number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (sistema) {
      const updateData: UpdateSistemaRequest = {
        id: sistema.id,
        ...formData
      };
      await saveSistemaApi.execute(() => SistemaService.update(sistema.id, updateData));
    } else {
      const createData: CreateSistemaRequest = {
        name: formData.name,
        description: formData.description,
        redirectUris: formData.redirectUris,
        audience: formData.audience
      };
      await saveSistemaApi.execute(() => SistemaService.create(createData));
    }
  };

  const isValid = formData.name && formData.redirectUris && formData.audience;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{sistema ? 'Editar Sistema' : 'Novo Sistema'}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Nome <span className="text-destructive">*</span>
              </label>
              <Input
                value={formData.name}
                onChange={(e) => handleInputChange('name', e.target.value)}
                error={!!getError('name')}
                helperText={getError('name')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Audience <span className="text-destructive">*</span>
              </label>
              <Input
                value={formData.audience}
                onChange={(e) => handleInputChange('audience', e.target.value)}
                error={!!getError('audience')}
                helperText={getError('audience')}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Descrição</label>
            <Input
              value={formData.description}
              onChange={(e) => handleInputChange('description', e.target.value)}
              error={!!getError('description')}
              helperText={getError('description')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Redirect URIs <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.redirectUris}
              onChange={(e) => handleInputChange('redirectUris', e.target.value)}
              error={!!getError('redirectUris')}
              helperText={getError('redirectUris')}
            />
          </div>

          {sistema && (
            <div className="flex items-center gap-2 pt-2">
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => handleInputChange('isActive', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                Ativo
              </label>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
            disabled={saveSistemaApi.isLoading}
          >
            Cancelar
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveSistemaApi.isLoading}
            disabled={!isValid}
          >
            {sistema ? 'Atualizar' : 'Criar'}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
