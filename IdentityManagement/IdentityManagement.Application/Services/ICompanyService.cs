using Archon.Application.Services;
using IdentityManagement.Application.Requests.Companies;
using IdentityManagement.Application.Responses.Companies;
using IdentityManagement.Domain.Entities;

namespace IdentityManagement.Application.Services
{
    public interface ICompanyService : ICrudService<Company>
    {
        Task<CompanyResponse> CreateCompany(CreateCompanyRequest request, CancellationToken cancellationToken = default);

        Task<CompanyResponse> UpdateCompany(long id, UpdateCompanyRequest request, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<CompanyResponse>> GetActiveCompanies(CancellationToken cancellationToken = default);
    }
}
