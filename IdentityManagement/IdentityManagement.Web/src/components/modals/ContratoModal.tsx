import { useState, useEffect } from 'react';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalFooter,
  Input,
  Button,
  Switch,
  SearchableSelect,
  useApi,
  toast,
  useFormErrors
} from 'd-rts';
import { ContratoService } from '../../services/contratoService';
import { EmpresaService } from '../../services/empresaService';
import { SistemaService } from '../../services/sistemaService';
import type { Contrato, CreateContratoRequest, UpdateContratoRequest } from '../../types/contrato';
import type { Empresa } from '../../types/empresa';
import type { Sistema } from '../../types/sistema';

interface ContratoModalProps {
  isOpen: boolean;
  onClose: () => void;
  contrato?: Contrato;
  onSuccess: () => void;
}

export default function ContratoModal({
  isOpen,
  onClose,
  contrato,
  onSuccess
}: ContratoModalProps) {
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [empresas, setEmpresas] = useState<Empresa[]>([]);
  const [sistemas, setSistemas] = useState<Sistema[]>([]);
  const [formData, setFormData] = useState({
    empresaId: 0,
    sistemaId: 0,
    startDate: new Date().toISOString().split('T')[0],
    endDate: '',
    isActive: true,
    accessTokenLifetime: 3600,
    refreshTokenLifetime: 2592000
  });

  const loadEmpresasApi = useApi({
    onSuccess: (data: Empresa[]) => {
      setEmpresas(data);
    },
    onError: () => {
      toast({
        title: 'Erro',
        description: 'Erro ao carregar empresas',
        variant: 'destructive',
      });
    }
  });

  const loadSistemasApi = useApi({
    onSuccess: (data: Sistema[]) => {
      setSistemas(data);
    },
    onError: () => {
      toast({
        title: 'Erro',
        description: 'Erro ao carregar sistemas',
        variant: 'destructive',
      });
    }
  });

  const saveContratoApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: contrato ? 'Contrato atualizado com sucesso' : 'Contrato criado com sucesso',
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
      loadEmpresasApi.execute(() => EmpresaService.getActive());
      loadSistemasApi.execute(() => SistemaService.getActive());

      if (contrato) {
        setFormData({
          empresaId: contrato.empresa?.id || 0,
          sistemaId: contrato.sistema?.id || 0,
          startDate: contrato.startDate.split('T')[0],
          endDate: contrato.endDate?.split('T')[0] || '',
          isActive: contrato.isActive,
          accessTokenLifetime: contrato.accessTokenLifetime,
          refreshTokenLifetime: contrato.refreshTokenLifetime
        });
      } else {
        setFormData({
          empresaId: 0,
          sistemaId: 0,
          startDate: new Date().toISOString().split('T')[0],
          endDate: '',
          isActive: true,
          accessTokenLifetime: 3600,
          refreshTokenLifetime: 2592000
        });
      }
    }
  }, [isOpen, contrato]);

  const handleInputChange = (field: string, value: string | boolean | number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (contrato) {
      const updateData: UpdateContratoRequest = {
        id: contrato.id,
        empresaId: formData.empresaId,
        sistemaId: formData.sistemaId,
        startDate: formData.startDate,
        endDate: formData.endDate || undefined,
        isActive: formData.isActive,
        accessTokenLifetime: formData.accessTokenLifetime,
        refreshTokenLifetime: formData.refreshTokenLifetime
      };
      await saveContratoApi.execute(() => ContratoService.update(contrato.id, updateData));
    } else {
      const createData: CreateContratoRequest = {
        empresaId: formData.empresaId,
        sistemaId: formData.sistemaId,
        startDate: formData.startDate,
        endDate: formData.endDate || undefined,
        accessTokenLifetime: formData.accessTokenLifetime,
        refreshTokenLifetime: formData.refreshTokenLifetime
      };
      await saveContratoApi.execute(() => ContratoService.create(createData));
    }
  };

  const isValid = formData.empresaId > 0 && formData.sistemaId > 0 && formData.startDate;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="2xl">
        <ModalHeader>
          <ModalTitle>{contrato ? 'Editar Contrato' : 'Novo Contrato'}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Empresa <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={empresas.map((empresa) => ({
                  label: empresa.nome,
                  value: empresa.id.toString()
                }))}
                value={formData.empresaId.toString()}
                onValueChange={(value) => handleInputChange('empresaId', parseInt(value))}
                placeholder="Selecione uma empresa"
                searchPlaceholder="Pesquisar empresa..."
                disabled={!!contrato || loadEmpresasApi.isLoading}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Sistema <span className="text-destructive">*</span>
              </label>
              <SearchableSelect
                options={sistemas.map((sistema) => ({
                  label: sistema.name,
                  value: sistema.id.toString()
                }))}
                value={formData.sistemaId.toString()}
                onValueChange={(value) => handleInputChange('sistemaId', parseInt(value))}
                placeholder="Selecione um sistema"
                searchPlaceholder="Pesquisar sistema..."
                disabled={!!contrato || loadSistemasApi.isLoading}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Data de Início <span className="text-destructive">*</span>
              </label>
              <Input
                type="date"
                value={formData.startDate}
                onChange={(e) => handleInputChange('startDate', e.target.value)}
                error={!!getError('startDate')}
                helperText={getError('startDate')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">Data de Término</label>
              <Input
                type="date"
                value={formData.endDate}
                onChange={(e) => handleInputChange('endDate', e.target.value)}
                error={!!getError('endDate')}
                helperText={getError('endDate')}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Duração do Access Token (segundos) <span className="text-destructive">*</span>
              </label>
              <Input
                type="number"
                value={formData.accessTokenLifetime}
                onChange={(e) => handleInputChange('accessTokenLifetime', parseInt(e.target.value) || 0)}
                error={!!getError('accessTokenLifetime')}
                helperText={getError('accessTokenLifetime')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                Duração do Refresh Token (segundos) <span className="text-destructive">*</span>
              </label>
              <Input
                type="number"
                value={formData.refreshTokenLifetime}
                onChange={(e) => handleInputChange('refreshTokenLifetime', parseInt(e.target.value) || 0)}
                error={!!getError('refreshTokenLifetime')}
                helperText={getError('refreshTokenLifetime')}
              />
            </div>
          </div>

          {contrato && (
            <div className="flex items-center gap-2 pt-2">
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => handleInputChange('isActive', checked)}
              />
              <label className="text-sm font-medium cursor-pointer">
                Contrato Ativo
              </label>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
            disabled={saveContratoApi.isLoading}
          >
            Cancelar
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveContratoApi.isLoading}
            disabled={!isValid}
          >
            {contrato ? 'Atualizar' : 'Criar'}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
