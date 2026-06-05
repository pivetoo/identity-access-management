using Archon.Application.Services;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IContractService : ICrudService<Contract>
    {
        Task<ContractSummaryResponse> CreateContract(CreateContractRequest request, string setupBaseUrl, CancellationToken cancellationToken = default);

        Task<Contract> CreateContractCore(CreateContractRequest request, CancellationToken cancellationToken = default);

        Task<ContractSummaryResponse> UpdateContract(long id, UpdateContractRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSummaryResponse>> GetByCompanyId(long companyId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSummaryResponse>> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSummaryResponse>> GetActive(CancellationToken cancellationToken = default);

        Task<Contract?> GetByCompanyAndSystemApplication(long companyId, long systemApplicationId, CancellationToken cancellationToken = default);

        Task<string?> GetUserRoleNameForContract(long userId, long contractId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSelectionResponseItem>> GetActiveContractSelectionsByUserId(long userId, long? systemApplicationId = null, CancellationToken cancellationToken = default);

        Task<Contract?> GetByIdWithRelations(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Contract>> GetActiveContractsByUserId(long userId, CancellationToken cancellationToken = default);

        Task ResendAdminInvitation(long contractId, string setupBaseUrl, CancellationToken cancellationToken = default);
    }
}
