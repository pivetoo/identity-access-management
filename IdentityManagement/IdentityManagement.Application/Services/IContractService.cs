using Archon.Application.Services;
using IdentityManagement.Application.Requests.Contracts;
using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface IContractService : ICrudService<Contract>
    {
        Task<ContractSummaryResponse> CreateContract(CreateContractRequest request, CancellationToken cancellationToken = default);

        Task<ContractSummaryResponse> UpdateContract(long id, UpdateContractRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSummaryResponse>> GetByCompanyId(long companyId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSummaryResponse>> GetByApplicationId(long applicationId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSummaryResponse>> GetActive(CancellationToken cancellationToken = default);

        Task<Contract?> GetByCompanyAndApplication(long companyId, long applicationId, CancellationToken cancellationToken = default);

        Task<string?> GetUserRoleNameForContract(long userId, long contractId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ContractSelectionResponseItem>> GetActiveContractSelectionsByUserId(long userId, CancellationToken cancellationToken = default);

        Task<Contract?> GetByIdWithRelations(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Contract>> GetActiveContractsByUserId(long userId, CancellationToken cancellationToken = default);

        Task<Contract?> GetByClientId(string clientId, CancellationToken cancellationToken = default);

        Task<ContractSecretsResponse?> GetContractSecrets(long id, CancellationToken cancellationToken = default);

        string GenerateClientId();

        string GenerateClientSecret();
    }
}
