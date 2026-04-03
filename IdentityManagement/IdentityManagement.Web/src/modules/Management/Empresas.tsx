import { useState, useEffect } from 'react';
import { ChevronDown, Filter } from 'lucide-react';
import { PageLayout, DataTable, Badge, Button, ConfirmModal, TableToolbar, Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, toast, useApi } from 'd-rts';
import type { DataTableColumn } from 'd-rts';
import { EmpresaService } from '../../services/empresaService';
import type { Empresa } from '../../types/empresa';
import type { PaginatedResult } from 'd-rts';
import EmpresaModal from '../../components/modals/EmpresaModal';

export default function Empresas() {
  const [selectedEmpresas, setSelectedEmpresas] = useState<Empresa[]>([]);
  const [empresas, setEmpresas] = useState<Empresa[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEmpresa, setEditingEmpresa] = useState<Empresa | undefined>();
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const pageSize = 30;

  const loadEmpresasApi = useApi({
    onSuccess: (data: PaginatedResult<Empresa>) => {
      setEmpresas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const loadMoreEmpresasApi = useApi({
    onSuccess: (data: PaginatedResult<Empresa>) => {
      setEmpresas(prev => [...prev, ...data.data]);
      setHasMore(data.data.length === pageSize);
    }
  });

  const deleteEmpresaApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: 'Sucesso',
        description: 'Empresa excluida com sucesso',
      });
      loadEmpresas(true);
      setSelectedEmpresas([]);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => {
      setIsConfirmDeleteOpen(false);
    }
  });

  const loadEmpresas = async (reset = false) => {
    if (reset) {
      setEmpresas([]);
    }
    await loadEmpresasApi.execute(() =>
      EmpresaService.getAll({
        page: 1,
        pageSize: pageSize,
        orderBy: 'nome'
      })
    );
  };

  const loadMoreEmpresas = async () => {
    const currentPage = Math.floor(empresas.length / pageSize) + 1;
    await loadMoreEmpresasApi.execute(() =>
      EmpresaService.getAll({
        page: currentPage + 1,
        pageSize: pageSize,
        orderBy: 'nome'
      })
    );
  };

  useEffect(() => {
    loadEmpresas(true);
  }, []);

  const handleAddEmpresa = () => {
    setEditingEmpresa(undefined);
    setIsModalOpen(true);
  };

  const handleEditEmpresa = () => {
    if (selectedEmpresas.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione uma empresa para editar',
      });
      return;
    }
    if (selectedEmpresas.length > 1) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione apenas uma empresa para editar',
      });
      return;
    }
    setEditingEmpresa(selectedEmpresas[0]);
    setIsModalOpen(true);
  };

  const handleDeleteEmpresa = () => {
    if (selectedEmpresas.length === 0) {
      toast({
        variant: 'warning',
        title: 'Atenção',
        description: 'Selecione uma ou mais empresas para excluir',
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    for (const empresa of selectedEmpresas) {
      await deleteEmpresaApi.execute(() => EmpresaService.delete(empresa.id));
    }
  };

  const handleRefresh = () => {
    loadEmpresas(true);
  };

  const columns: DataTableColumn<Empresa>[] = [
    {
      key: 'nome',
      title: 'Nome',
      dataIndex: 'nome',
      sortable: true,
    },
    {
      key: 'nomeFantasia',
      title: 'Nome Fantasia',
      dataIndex: 'nomeFantasia',
    },
    {
      key: 'documento',
      title: 'CNPJ',
      dataIndex: 'documento',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
      }
    },
    {
      key: 'email',
      title: 'E-mail',
      dataIndex: 'email',
    },
    {
      key: 'telefone',
      title: 'Telefone',
      dataIndex: 'telefone',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3');
      }
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

  const handleSelectionChange = (selected: Empresa[]) => {
    setSelectedEmpresas(selected);
  };

  const handleModalSuccess = () => {
    setSelectedEmpresas([]);
    loadEmpresas(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingEmpresa(undefined);
  };

  const filteredEmpresas = empresas.filter((empresa) => {
    const search = searchTerm.trim().toLowerCase();
    if (!search) {
      return true;
    }

    return [empresa.nome, empresa.nomeFantasia, empresa.email, empresa.documento]
      .some((value) => value.toLowerCase().includes(search));
  });

  const previewEmpresa = selectedEmpresas.length === 1 ? selectedEmpresas[0] : null;

  return (
    <>
      <PageLayout
        title="Empresas"
        subtitle="Cadastre e mantenha as organizações vinculadas ao Identity Management."
        onAdd={handleAddEmpresa}
        onEdit={handleEditEmpresa}
        onDelete={handleDeleteEmpresa}
        onRefresh={handleRefresh}
        selectedRowsCount={selectedEmpresas.length}
      >
        <div className="space-y-4">
          <TableToolbar
            searchValue={searchTerm}
            onSearchChange={setSearchTerm}
            searchPlaceholder="Buscar por nome, fantasia, e-mail ou CNPJ"
            rightSlot={<Button variant="outline" size="sm" icon={<Filter className="h-4 w-4" />} />}
          />

          <DataTable
            columns={columns}
            data={filteredEmpresas}
            loading={loadEmpresasApi.isLoading || deleteEmpresaApi.isLoading}
            rowKey="id"
            selectable
            selectedRows={selectedEmpresas}
            onSelectionChange={handleSelectionChange}
          />

          {hasMore && (
            <div className="mt-4 flex justify-end">
              <Button
                variant="outline"
                onClick={loadMoreEmpresas}
                loading={loadMoreEmpresasApi.isLoading}
              >
                Carregar mais
              </Button>
            </div>
          )}
        </div>

        <EmpresaModal
          isOpen={isModalOpen}
          onClose={handleModalClose}
          empresa={editingEmpresa}
          onSuccess={handleModalSuccess}
        />

        <ConfirmModal
          open={isConfirmDeleteOpen}
          onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
          onConfirm={handleConfirmDelete}
          title="Confirmar Exclusão"
          description={
            selectedEmpresas.length === 1
              ? `Deseja excluir a empresa "${selectedEmpresas[0]?.nome}"?`
              : `Deseja excluir ${selectedEmpresas.length} empresas selecionadas?`
          }
          confirmText="Excluir"
          variant="danger"
          loading={deleteEmpresaApi.isLoading}
        />
      </PageLayout>

      <Sheet open={!!previewEmpresa} onOpenChange={(open) => !open && setSelectedEmpresas([])}>
        <SheetContent side="right" className="w-full sm:max-w-md">
          {previewEmpresa ? (
            <div className="flex h-full flex-col">
              <SheetHeader className="space-y-4 border-b border-border/70 pb-5">
                <div className="inline-flex w-fit items-center rounded-full border border-primary/15 bg-primary/5 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-primary">
                  Empresa
                </div>
                <div className="space-y-2">
                  <SheetTitle>{previewEmpresa.nome}</SheetTitle>
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge variant={previewEmpresa.isActive ? 'success' : 'destructive'}>
                      {previewEmpresa.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    <span className="text-xs font-medium text-muted-foreground">
                      {previewEmpresa.documento || 'CNPJ não informado'}
                    </span>
                  </div>
                </div>
                <SheetDescription>
                  Dados cadastrais e informações de contato da empresa selecionada.
                </SheetDescription>
              </SheetHeader>

              <div className="mt-6 flex-1 space-y-4 overflow-y-auto">
                <details open className="group border-b border-border/70 pb-4">
                  <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3">
                    <div>
                      <div className="text-base font-bold tracking-[-0.01em] text-primary">Dados cadastrais</div>
                      <div className="text-xs text-muted-foreground">Identificação principal da empresa</div>
                    </div>
                    <ChevronDown className="h-4 w-4 text-muted-foreground transition-transform group-open:rotate-180" />
                  </summary>
                  <div className="grid gap-4 border-t border-border/60 px-4 pt-4 sm:grid-cols-2">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Razão social</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewEmpresa.nome || '-'}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">CNPJ</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewEmpresa.documento || '-'}</div>
                    </div>
                    <div className="sm:col-span-2">
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Nome fantasia</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewEmpresa.nomeFantasia || '-'}</div>
                    </div>
                  </div>
                </details>

                <details open className="group pb-2">
                  <summary className="flex cursor-pointer list-none items-center justify-between px-4 py-3">
                    <div>
                      <div className="text-base font-bold tracking-[-0.01em] text-primary">Contato</div>
                      <div className="text-xs text-muted-foreground">Canais principais da empresa</div>
                    </div>
                    <ChevronDown className="h-4 w-4 text-muted-foreground transition-transform group-open:rotate-180" />
                  </summary>
                  <div className="grid gap-4 border-t border-border/60 px-4 pt-4">
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">Telefone</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewEmpresa.telefone || '-'}</div>
                    </div>
                    <div>
                      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/70">E-mail</div>
                      <div className="mt-1 text-sm font-medium text-foreground">{previewEmpresa.email || '-'}</div>
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
