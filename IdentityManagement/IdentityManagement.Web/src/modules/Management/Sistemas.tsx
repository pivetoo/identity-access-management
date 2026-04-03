import { useState, useEffect } from 'react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, Sheet, SheetContent, SheetPreviewField, SheetPreviewGrid, SheetPreviewHeader, SheetPreviewSection, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { SistemaService } from '../../services/sistemaService';
import type { Sistema } from '../../types/sistema';
import type { PaginatedResult } from 'd-rts';
import SistemaModal from '../../components/modals/SistemaModal';

export default function Sistemas() {
  const [selectedSistemas, setSelectedSistemas] = useState<Sistema[]>([]);
  const [previewSistema, setPreviewSistema] = useState<Sistema | null>(null);
  const [sistemas, setSistemas] = useState<Sistema[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingSistema, setEditingSistema] = useState<Sistema | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const pageSize = 30;

  const loadSistemasApi = useApi({
    onSuccess: (data: PaginatedResult<Sistema>) => {
      setSistemas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreSistemasApi = useApi({
    onSuccess: (data: PaginatedResult<Sistema>) => {
      setSistemas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteSistemaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Sistema excluido com sucesso',
      });
      loadSistemas(true);
      setSelectedSistemas([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const loadSistemas = async (reset = false) => {
    if (reset) {
      setSistemas([]);
    }
    await loadSistemasApi.execute(() =>
      SistemaService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'name'
      })
    );
  };

  const loadMoreSistemas = async () => {
    const currentPage = Math.floor(sistemas.length / pageSize) + 1;
    await loadMoreSistemasApi.execute(() =>
      SistemaService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'name'
      })
    );
  };

  useEffect(() => {
    loadSistemas(true);
  }, []);

  const handleAddSistema = () => {
    setEditingSistema(undefined);
    setIsModalOpen(true);
  };

  const handleEditSistema = () => {
    if (selectedSistemas.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um sistema para editar',
      });
      return;
    }
    if (selectedSistemas.length > 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione apenas um sistema para editar',
      });
      return;
    }
    setEditingSistema(selectedSistemas[0]);
    setIsModalOpen(true);
  };

  const handleDeleteSistema = () => {
    if (selectedSistemas.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione um ou mais sistemas para excluir',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const sistema of selectedSistemas) {
      await deleteSistemaApi.execute(() => SistemaService.delete(sistema.id));
    }
  };

  const handleRefresh = () => {
    loadSistemas(true);
  };

  const columns: DataTableColumn<Sistema>[] = [
    {
      key: 'name',
      title: 'Nome',
      dataIndex: 'name',
      sortable: true,
    },
    {
      key: 'description',
      title: 'Descrição',
      dataIndex: 'description',
      render: (value: string) => value || '-'
    },
    {
      key: 'audience',
      title: 'Audience',
      dataIndex: 'audience',
    },
    {
      key: 'isActive',
      title: 'Status',
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? 'Ativo' : 'Inativo'}
        </Badge>
      )
    },
  ];

  const handleSelectionChange = (selected: Sistema[]) => {
    setSelectedSistemas(selected);
  };

  const handleModalSuccess = () => {
    setSelectedSistemas([]);
    loadSistemas(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingSistema(undefined);
  };

  return (
    <>
      <PageLayout
        title="Sistemas"
        subtitle="Gerencie aplicações integradas e suas configurações de autenticação."
        onAdd={handleAddSistema}
        onEdit={handleEditSistema}
        onDelete={handleDeleteSistema}
        onRefresh={handleRefresh}
        selectedRowsCount={selectedSistemas.length}
      >
        <DataTable
          columns={columns}
          data={sistemas}
          loading={loadSistemasApi.isLoading || deleteSistemaApi.isLoading}
          rowKey="id"
          selectable
          selectedRows={selectedSistemas}
          onSelectionChange={handleSelectionChange}
          onRowDoubleClick={setPreviewSistema}
        />

        {hasMore && (
          <div className="mt-4 flex justify-end">
            <Button
              variant="outline"
              onClick={loadMoreSistemas}
              loading={loadMoreSistemasApi.isLoading}
            >
              Carregar mais
            </Button>
          </div>
        )}

        <SistemaModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          sistema={editingSistema}
          onSuccess={handleModalSuccess}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title="Confirmar Exclusão"
          description={
            selectedSistemas.length === 1
              ? `Deseja excluir o sistema "${selectedSistemas[0]?.name}"?`
              : `Deseja excluir ${selectedSistemas.length} sistemas selecionados?`
          }
          confirmText="Excluir"
          variant="danger"
          loading={deleteSistemaApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewSistema} onOpenChange={(open) => !open && setPreviewSistema(null)}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewSistema ? (
            <div className="flex h-full flex-col">
              <SheetPreviewHeader
                eyebrow="Sistema"
                title={previewSistema.name}
                meta={
                  <>
                    <Badge variant={previewSistema.isActive ? 'success' : 'destructive'}>
                      {previewSistema.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      {previewSistema.audience || 'Audience não informado'}
                    </span>
                  </>
                }
                description="Configurações principais e metadados do sistema selecionado."
              />

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <SheetPreviewSection title="Configuração" description="Identificação e estado do sistema">
                  <SheetPreviewGrid>
                    <SheetPreviewField label="Audience" value={previewSistema.audience || '-'} />
                    <SheetPreviewField
                      label="Situação"
                      value={
                        <Badge variant={previewSistema.isActive ? 'success' : 'destructive'}>
                          {previewSistema.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      }
                    />
                    <SheetPreviewField className="sm:col-span-2" label="Descrição" value={previewSistema.description || '-'} />
                  </SheetPreviewGrid>
                </SheetPreviewSection>

                <SheetPreviewSection title="Endpoints" description="URIs de retorno configuradas">
                  <SheetPreviewField label="Redirect URI" value={previewSistema.redirectUris || '-'} />
                </SheetPreviewSection>
              </div>

            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
