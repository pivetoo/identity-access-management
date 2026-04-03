using Archon.Infrastructure.Services;
using IdentityManagement.Application.Requests.Companies;
using IdentityManagement.Application.Responses.Companies;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityManagement.Infrastructure.Services
{
    public sealed class CompanyService : CrudService<Company>, ICompanyService
    {
        public CompanyService(DbContext dbContext) : base(dbContext)
        {
        }

        public async Task<CompanyResponse> CreateCompany(CreateCompanyRequest request, CancellationToken cancellationToken = default)
        {
            await EnsureUniqueCompany(request.Document, request.Email, null, cancellationToken);

            Company company = new Company(request.LegalName, request.TradeName, request.Document, request.Email, request.PhoneNumber);
            bool success = await Insert(cancellationToken, company);
            if (!success)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(company);
        }

        public async Task<CompanyResponse> UpdateCompany(long id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
        {
            if (id != request.Id)
            {
                throw new InvalidOperationException("Route id does not match body id.");
            }

            Company? company = await (
                from item in DbContext.Set<Company>().AsTracking()
                where item.Id == id
                select item)
                .FirstOrDefaultAsync(cancellationToken);

            if (company is null)
            {
                throw new InvalidOperationException("Company not found.");
            }

            await EnsureUniqueCompany(request.Document, request.Email, id, cancellationToken);

            company.Update(request.LegalName, request.TradeName, request.Document, request.Email, request.PhoneNumber, request.IsActive);

            Company? result = await Update(company, cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException(GetErrorMessages());
            }

            return ToResponse(result);
        }

        public async Task<IReadOnlyCollection<CompanyResponse>> GetActiveCompanies(CancellationToken cancellationToken = default)
        {
            List<CompanyResponse> companies = await (
                from company in DbContext.Set<Company>().AsNoTracking()
                where company.IsActive
                orderby company.LegalName
                select new CompanyResponse
                {
                    Id = company.Id,
                    LegalName = company.LegalName,
                    TradeName = company.TradeName,
                    Document = company.Document,
                    Email = company.Email,
                    PhoneNumber = company.PhoneNumber,
                    IsActive = company.IsActive,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return companies;
        }

        private async Task EnsureUniqueCompany(string document, string email, long? currentCompanyId, CancellationToken cancellationToken)
        {
            bool documentExists = await (
                from company in DbContext.Set<Company>().AsNoTracking()
                where company.Document == document && (!currentCompanyId.HasValue || company.Id != currentCompanyId.Value)
                select company.Id)
                .AnyAsync(cancellationToken);

            if (documentExists)
            {
                throw new InvalidOperationException("Document already exists.");
            }

            bool emailExists = await (
                from company in DbContext.Set<Company>().AsNoTracking()
                where company.Email == email && (!currentCompanyId.HasValue || company.Id != currentCompanyId.Value)
                select company.Id)
                .AnyAsync(cancellationToken);

            if (emailExists)
            {
                throw new InvalidOperationException("Email already exists.");
            }
        }

        private static CompanyResponse ToResponse(Company company)
        {
            return new CompanyResponse
            {
                Id = company.Id,
                LegalName = company.LegalName,
                TradeName = company.TradeName,
                Document = company.Document,
                Email = company.Email,
                PhoneNumber = company.PhoneNumber,
                IsActive = company.IsActive,
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt
            };
        }
    }
}
