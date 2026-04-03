import { useState, useEffect } from 'react';
import {
  Modal,
  ModalContent,
  ModalHeader,
  ModalTitle,
  ModalFooter,
  Button,
  SearchableSelect,
  useApi,
  toast,
  useFormErrors
} from 'd-rts';
import { UsuarioPerfilService } from '../../services/usuarioPerfilService';
import { UsuarioService } from '../../services/usuarioService';
import { PerfilService } from '../../services/perfilService';
import type { AssignUsuarioToPerfilRequest } from '../../types/usuarioPerfil';
import type { Usuario } from '../../types/usuario';
import type { PerfilSummaryViewModel } from '../../types/perfil';

interface UsuarioPerfilModalProps {
  isOpen: boolean;
  onClose: () => void;
  contratoId: number;
  onSuccess: () => void;
}

export default function UsuarioPerfilModal({
  isOpen,
  onClose,
  contratoId,
  onSuccess
}: UsuarioPerfilModalProps) {
  const { setErrors, clearErrors } = useFormErrors();
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [perfis, setPerfis] = useState<PerfilSummaryViewModel[]>([]);
  const [formData, setFormData] = useState({
    usuarioId: 0,
    perfilId: 0
  });

  const loadUsuariosApi = useApi({
    onSuccess: (data: any) => {
      setUsuarios(data.data || data);
    }
  });

  const loadPerfisApi = useApi({
    onSuccess: (data: PerfilSummaryViewModel[]) => {
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
    if (isOpen && contratoId) {
      loadUsuariosApi.execute(() => UsuarioService.getAll({ page: 1, pageSize: 1000 }));
      loadPerfisApi.execute(async () => {
        const data = await PerfilService.getByContrato(contratoId, { page: 1, pageSize: 1000 });
        return data.data;
      });

      setFormData({
        usuarioId: 0,
        perfilId: 0
      });
    }
  }, [isOpen, contratoId]);

  const handleInputChange = (field: string, value: number) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    const createData: AssignUsuarioToPerfilRequest = {
      usuarioId: formData.usuarioId,
      perfilId: formData.perfilId
    };
    await saveUsuarioPerfilApi.execute(() => UsuarioPerfilService.assign(createData));
  };

  const isValid = formData.usuarioId && formData.perfilId;

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>Vincular Usuário ao Perfil</ModalTitle>
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
              value={formData.usuarioId.toString()}
              onValueChange={(value) => handleInputChange('usuarioId', parseInt(value))}
              placeholder="Selecione um usuário"
              searchPlaceholder="Pesquisar usuário..."
              disabled={loadUsuariosApi.isLoading}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              Perfil <span className="text-destructive">*</span>
            </label>
            <SearchableSelect
              options={perfis.map(perfil => ({
                value: perfil.id.toString(),
                label: perfil.name
              }))}
              value={formData.perfilId.toString()}
              onValueChange={(value) => handleInputChange('perfilId', parseInt(value))}
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
