import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, useApi, toast, useFormErrors } from 'archon-ui';
import { EmpresaService } from '../../services/empresaService';
import type { Empresa, CreateEmpresaRequest, UpdateEmpresaRequest } from '../../types/empresa';

interface EmpresaModalProps {
  isOpen: boolean;
  onClose: () => void;
  empresa?: Empresa;
  onSuccess: () => void;
}

export default function EmpresaModal({
  isOpen,
  onClose,
  empresa,
  onSuccess
}: EmpresaModalProps) {
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [formData, setFormData] = useState({
    nome: '',
    nomeFantasia: '',
    documento: '',
    email: '',
    telefone: '',
    isActive: true
  });

  const saveEmpresaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: empresa ? 'Empresa atualizada com sucesso' : 'Empresa criada com sucesso',
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
      if (empresa) {
        setFormData({
          nome: empresa.nome,
          nomeFantasia: empresa.nomeFantasia,
          documento: empresa.documento,
          email: empresa.email,
          telefone: empresa.telefone,
          isActive: empresa.isActive
        });
      } else {
        setFormData({
          nome: '',
          nomeFantasia: '',
          documento: '',
          email: '',
          telefone: '',
          isActive: true
        });
      }
    }
  }, [isOpen, empresa]);

  const handleInputChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (empresa) {
      const updateData: UpdateEmpresaRequest = {
        id: empresa.id,
        ...formData
      };
      await saveEmpresaApi.execute(() => EmpresaService.update(empresa.id, updateData));
    } else {
      const createData: CreateEmpresaRequest = {
        nome: formData.nome,
        nomeFantasia: formData.nomeFantasia,
        documento: formData.documento,
        email: formData.email,
        telefone: formData.telefone
      };
      await saveEmpresaApi.execute(() => EmpresaService.create(createData));
    }
  };

  const isValid = formData.nome && formData.documento && formData.email;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{empresa ? 'Editar Empresa' : 'Nova Empresa'}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Nome <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.nome}
              onChange={(e) => handleInputChange('nome', e.target.value)}
              error={!!getError('nome')}
              helperText={getError('nome')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Nome Fantasia</label>
            <Input
              value={formData.nomeFantasia}
              onChange={(e) => handleInputChange('nomeFantasia', e.target.value)}
              error={!!getError('nomeFantasia')}
              helperText={getError('nomeFantasia')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              CNPJ <span className="text-destructive">*</span>
            </label>
            <Input
              value={formData.documento}
              onChange={(e) => handleInputChange('documento', e.target.value)}
              error={!!getError('documento')}
              helperText={getError('documento')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              E-mail <span className="text-destructive">*</span>
            </label>
            <Input
              type="email"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              error={!!getError('email')}
              helperText={getError('email')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">Telefone</label>
            <Input
              value={formData.telefone}
              onChange={(e) => handleInputChange('telefone', e.target.value)}
              error={!!getError('telefone')}
              helperText={getError('telefone')}
            />
          </div>

          {empresa && (
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
            disabled={saveEmpresaApi.isLoading}
          >
            Cancelar
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveEmpresaApi.isLoading}
            disabled={!isValid}
          >
            {empresa ? 'Atualizar' : 'Criar'}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
