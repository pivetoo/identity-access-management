import { useState, useEffect } from 'react';
import { Modal, ModalContent, ModalHeader, ModalTitle, ModalFooter, Input, Button, Switch, useApi, toast, useFormErrors, useI18n } from 'archon-ui';
import { UserService } from '../../services/userService';
import type { User, CreateUserRequest } from '../../types/user';

interface UserFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  usuario?: User;
  onSuccess: () => void;
}

export default function UserFormModal({
  isOpen,
  onClose,
  usuario,
  onSuccess
}: UserFormModalProps) {
  const { t } = useI18n()
  const { getError, setErrors, clearErrors } = useFormErrors();
  const [formData, setFormData] = useState({
    username: '',
    email: '',
    password: '',
    name: '',
    isActive: true
  });

  const saveUsuarioApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: usuario ? t('user.form.toast.updated') : t('user.form.toast.created'),
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
    if (isOpen) {
      if (usuario) {
        setFormData({
          username: usuario.username,
          email: usuario.email,
          password: '',
          name: usuario.name,
          isActive: usuario.isActive
        });
      } else {
        setFormData({
          username: '',
          email: '',
          password: '',
          name: '',
          isActive: true
        });
      }
    }
  }, [isOpen, usuario]);

  const handleInputChange = (field: string, value: string | boolean) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleSave = async () => {
    if (usuario) {
      const updateData: any = {
        id: usuario.id,
        username: formData.username,
        email: formData.email,
        name: formData.name,
        isActive: formData.isActive
      };

      if (formData.password) {
        updateData.password = formData.password;
      }

      await saveUsuarioApi.execute(() => UserService.update(usuario.id, updateData));
    } else {
      const createData: CreateUserRequest = {
        username: formData.username,
        email: formData.email,
        password: formData.password,
        name: formData.name
      };
      await saveUsuarioApi.execute(() => UserService.create(createData));
    }
  };

  const isValid = formData.username && formData.email && (usuario || formData.password);

  return (
    <Modal open={isOpen} onOpenChange={onClose}>
      <ModalContent size="xl">
        <ModalHeader>
          <ModalTitle>{usuario ? t('user.form.editTitle') : t('user.form.createTitle')}</ModalTitle>
        </ModalHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('user.field.username')} <span className="text-destructive">*</span>
              </label>
              <Input
                value={formData.username}
                onChange={(e) => handleInputChange('username', e.target.value)}
                error={!!getError('username')}
                helperText={getError('username')}
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-sm font-medium">
                {t('common.field.email')} <span className="text-destructive">*</span>
              </label>
              <Input
                type="email"
                value={formData.email}
                onChange={(e) => handleInputChange('email', e.target.value)}
                error={!!getError('email')}
                helperText={getError('email')}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">{t('user.field.fullName')}</label>
            <Input
              value={formData.name}
              onChange={(e) => handleInputChange('name', e.target.value)}
              error={!!getError('name')}
              helperText={getError('name')}
            />
          </div>

          <div className="flex flex-col gap-2">
            <label className="text-sm font-medium">
              {usuario ? t('user.form.newPasswordLabel') : t('common.field.password')}
              {!usuario && <span className="text-destructive"> *</span>}
            </label>
            <Input
              type="password"
              value={formData.password}
              onChange={(e) => handleInputChange('password', e.target.value)}
              error={!!getError('password')}
              helperText={getError('password')}
            />
          </div>

          {usuario && (
            <div className="flex gap-6 pt-2">
              <div className="flex items-center gap-2">
                <Switch
                  checked={formData.isActive}
                  onCheckedChange={(checked) => handleInputChange('isActive', checked)}
                />
                <label className="text-sm font-medium cursor-pointer">
                  {t('user.form.activeLabel')}
                </label>
              </div>
            </div>
          )}
        </div>

        <ModalFooter>
          <Button
            variant="outline"
            onClick={onClose}
            disabled={saveUsuarioApi.isLoading}
          >
            {t('common.action.cancel')}
          </Button>
          <Button
            variant="primary"
            onClick={handleSave}
            loading={saveUsuarioApi.isLoading}
            disabled={!isValid}
          >
            {usuario ? t('common.action.update') : t('common.action.create')}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}
