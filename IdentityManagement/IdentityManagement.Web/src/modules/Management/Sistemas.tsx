import { useState, useEffect } from 'react';
import { ChevronDown } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { SistemaService } from '../../services/sistemaService';
import type { Sistema } from '../../types/sistema';
import type { PaginatedResult } from 'd-rts';
import SistemaModal from '../../components/modals/SistemaModal';

export default function Sistemas() {
  const [selectedSistemas, setSelectedSistemas] = useState<Sistema[]>([]);
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

  const previewSistema = selectedSistemas.length === 1 ? selectedSistemas[0] : null;

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

      <Sheet open={!!previewSistema} onOpenChange={(open) => !open && setSelectedSistemas([])}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewSistema ? (
            <div className="flex h-full flex-col">
              <SheetHeader className="space-y-4 border-b border-border/70 pb-5">
                <div className="inline-flex w-fit items-center rounded-full border border-primary/15 bg-primary/5 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-primary">
                  Sistema
                </div>
                <div className="space-y-2">
                  <SheetTitle>{previewSistema.name}</SheetTitle>
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant={previewSistema.isActive ? 'success' : 'destructive'}>
                      {previewSistema.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      {previewSistema.audience || 'Audience não informado'}
                    </span>
                  </div>
                </div>
                <SheetDescription>
                  Configurações principais e metadados do sistema selecionado.
                </SheetDescription>
              </SheetHeader>

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <details open className="group border-b border-border/70 pb-4">
                  <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3">
                    <div>
                      <div className="text-base font-bold tracking-[-0.01em] text-primary">Configuração</div>
                      <div className="text-xs text-muted-foreground">Identificação e estado do sistema</div>
                    </div>
                    <ChevronDown className="h-4 w-4 text-muted-foreground transition-transform group-open:rotate-180" />
                  </summary>
                  <div className="grid gap-4 border-t border-border/60 px-4 pt-4 sm:grid-cols-2">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Audience</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewSistema.audience || '-'}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Situação</div>
                      <div className="mt-2">
                        <Badge variant={previewSistema.isActive ? 'success' : 'destructive'}>
                          {previewSistema.isActive ? 'Ativo' : 'Inativo'}
                        </Badge>
                      </div>
                    </div>
                    <div className="sm:col-span-2">
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Descrição</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewSistema.description || '-'}</div>
                    </div>
                  </div>
                </details>

                <details open className="group pb-2">
                  <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3">
                    <div>
                      <div className="text-base font-bold tracking-[-0.01em] text-primary">Endpoints</div>
                      <div className="text-xs text-muted-foreground">URIs de retorno configuradas</div>
                    </div>
                    <ChevronDown className="h-4 w-4 text-muted-foreground transition-transform group-open:rotate-180" />
                  </summary>
                  <div className="border-t border-border/60 px-4 pt-4">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Redirect URI</div>
                      <div className="mt-1 break-all text-sm font-medium text-foreground">{previewSistema.redirectUris || '-'}</div>
                    </div>
                  </div>
                </details>
              </div>

            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </>
  );
}
