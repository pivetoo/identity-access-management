import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import { PageLayout, DataTable, Badge, ConfirmModal, FilterPanel, TableToolbar, toast, useApi, useI18n } from 'archon-ui';
import type { DataTableColumn, FilterSection, PaginatedResult } from 'archon-ui';
import { CompanyService } from '../../../services/companyService';
import type { Company } from '../../../types/company';
import CompanyFormModal from '../../../components/modals/CompanyFormModal';

export default function Companies() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [empresas, setEmpresas] = useState<Company[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [selectedEmpresa, setSelectedEmpresa] = useState<Company | null>(null);
  const [editingEmpresa, setEditingEmpresa] = useState<Company | undefined>();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);

  const { execute: fetchEmpresas, loading, pagination } = useApi<PaginatedResult<Company>>({ showErrorMessage: true });
  const deleteApi = useApi({
    onSuccess: () => {
      toast({
        variant: 'success',
        title: t('common.toast.successTitle'),
        description: t('company.list.toast.deleted'),
      });
      void loadEmpresas();
      setSelectedEmpresa(null);
      setIsConfirmDeleteOpen(false);
    },
    onError: () => setIsConfirmDeleteOpen(false),
  });

  const loadEmpresas = async () => {
    const result = await fetchEmpresas(() =>
      CompanyService.getAll({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        orderBy: 'id',
      }),
    );
    if (result) {
      const items: Company[] = result.data ?? [];
      const filtered = items.filter((empresa) => {
        if (statusFilter === 'active') return empresa.isActive;
        if (statusFilter === 'inactive') return !empresa.isActive;
        return true;
      });
      setEmpresas(filtered);
    }
  };

  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timeout);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter]);

  useEffect(() => {
    void loadEmpresas();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, debouncedSearch, statusFilter]);

  const filterSections: FilterSection[] = useMemo(
    () => [
      {
        key: 'status',
        label: t('common.column.status'),
        value: statusFilter,
        onChange: setStatusFilter,
        options: [
          { value: 'active', label: t('common.filter.activeOnly') },
          { value: 'inactive', label: t('common.filter.inactiveOnly') },
        ],
        allLabel: t('common.filter.all'),
      },
    ],
    [statusFilter, t],
  );

  const clearFilters = () => {
    setStatusFilter('');
  };

  const handleAddEmpresa = () => {
    navigate('/management/clients/new');
  };

  const handleEditEmpresa = () => {
    if (!selectedEmpresa) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('company.list.validation.selectOneToEdit'),
      });
      return;
    }
    setEditingEmpresa(selectedEmpresa);
    setIsModalOpen(true);
  };

  const handleDeleteEmpresa = () => {
    if (!selectedEmpresa) {
      toast({
        variant: 'warning',
        title: t('common.toast.warningTitle'),
        description: t('company.list.validation.selectToDelete'),
      });
      return;
    }
    setIsConfirmDeleteOpen(true);
  };

  const handleConfirmDelete = async () => {
    if (!selectedEmpresa) return;
    await deleteApi.execute(() => CompanyService.delete(selectedEmpresa.id));
  };

  const handleOpenDetail = (id: number) => {
    navigate(`/management/clients/${id}`);
  };

  const columns: DataTableColumn<Company>[] = [
    {
      key: 'nome',
      title: t('common.column.name'),
      dataIndex: 'legalName',
      sortable: true,
    },
    {
      key: 'tenantId',
      title: t('company.field.tenantId'),
      dataIndex: 'tenantId',
      hiddenBelow: 'sm',
      render: (value?: string) => (
        <span className="font-mono text-xs text-muted-foreground">{value || '-'}</span>
      ),
    },
    {
      key: 'documento',
      title: t('company.field.document'),
      dataIndex: 'document',
      hiddenBelow: 'md',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
      },
    },
    {
      key: 'email',
      title: t('common.field.email'),
      dataIndex: 'email',
      hiddenBelow: 'lg',
    },
    {
      key: 'telefone',
      title: t('common.field.phoneNumber'),
      dataIndex: 'phoneNumber',
      hiddenBelow: 'lg',
      render: (value: string) => {
        if (!value) return '-';
        return value.replace(/^(\d{2})(\d{5})(\d{4})$/, '($1) $2-$3');
      },
    },
    {
      key: 'isActive',
      title: t('common.column.status'),
      dataIndex: 'isActive',
      render: (value: boolean) => (
        <Badge variant={value ? 'success' : 'destructive'}>
          {value ? t('common.status.active') : t('common.status.inactive')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      title: '',
      dataIndex: 'id',
      render: (_value, record) => (
        <button
          type="button"
          className="inline-flex items-center gap-1 text-xs font-semibold text-primary hover:underline"
          onClick={(e) => {
            e.stopPropagation();
            handleOpenDetail(record.id);
          }}
        >
          {t('company.list.openDetail')}
          <ChevronRight className="h-3.5 w-3.5" />
        </button>
      ),
    },
  ];

  return (
    <PageLayout
      title={t('company.list.title')}
      subtitle={t('company.list.subtitle')}
      onAdd={handleAddEmpresa}
      onEdit={handleEditEmpresa}
      onDelete={handleDeleteEmpresa}
      onRefresh={() => void loadEmpresas()}
      selectedRowsCount={selectedEmpresa ? 1 : 0}
    >
      <TableToolbar
        searchValue={search}
        onSearchChange={setSearch}
        searchPlaceholder={t('common.action.search')}
        rightSlot={<FilterPanel sections={filterSections} onClearAll={clearFilters} />}
        className="mb-3"
      />

      <DataTable
        columns={columns}
        data={empresas}
        rowKey="id"
        loading={loading || deleteApi.isLoading}
        selectable
        selectedRows={selectedEmpresa ? [selectedEmpresa] : []}
        onSelectionChange={(rows) => setSelectedEmpresa(rows[0] ?? null)}
        onRowDoubleClick={(record) => handleOpenDetail(record.id)}
        emptyText={t('common.state.empty')}
        pageSize={pageSize}
        pageSizeOptions={[10, 20, 50]}
        totalCount={pagination?.totalCount}
        page={page}
        onPageChange={setPage}
        onPageSizeChange={(s) => {
          setPageSize(s);
          setPage(1);
        }}
      />

      <CompanyFormModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingEmpresa(undefined);
        }}
        company={editingEmpresa}
        onSuccess={() => {
          setIsModalOpen(false);
          setEditingEmpresa(undefined);
          setSelectedEmpresa(null);
          void loadEmpresas();
        }}
      />

      <ConfirmModal
        open={isConfirmDeleteOpen}
        onOpenChange={(open) => setIsConfirmDeleteOpen(open)}
        onConfirm={handleConfirmDelete}
        title={t('common.confirm.deleteTitle')}
        description={
          selectedEmpresa
            ? t('company.list.confirmDeleteSingle').replace('{0}', selectedEmpresa.legalName)
            : ''
        }
        confirmText={t('common.action.delete')}
        variant="danger"
        loading={deleteApi.isLoading}
      />
    </PageLayout>
  );
}
