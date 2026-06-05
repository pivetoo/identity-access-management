using IdentityManagement.Application.Requests.SystemIntegrations;
using IdentityManagement.Application.Responses.SystemIntegrations;
using IdentityManagement.Application.Services;
using IdentityManagement.Domain.Entities;
using IdentityManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityManagement.IntegrationTests.Services
{
    [TestFixture]
    public sealed class SystemIntegrationServiceIntegrationTests : IntegrationTestBase
    {
        // Semeia um SystemApplication diretamente no DbContext para isolar o teste
        // do fluxo de onboarding e garantir que a FK exista antes do CreateAsync.
        private static async Task<SystemApplication> SeedSystemApplicationAsync(DbContext dbContext, string name, string audience)
        {
            SystemApplication application = new SystemApplication(
                name: name,
                description: $"Test application: {name}",
                audience: audience);

            await dbContext.Set<SystemApplication>().AddAsync(application);
            await dbContext.SaveChangesAsync();

            return application;
        }

        [Test]
        public async Task Create_with_parameters_then_GetBySystem_returns_it()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISystemIntegrationService service = sp.GetRequiredService<ISystemIntegrationService>();

                SystemApplication application = await SeedSystemApplicationAsync(dbContext, "IntegrationPlatform", "integration-platform-aud-create");

                UpsertSystemIntegrationRequest request = new UpsertSystemIntegrationRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "integration-platform",
                    BaseUrl = "https://integration.example.com",
                    IsActive = true,
                    Parameters = new List<SystemIntegrationParameterRequest>
                    {
                        new SystemIntegrationParameterRequest
                        {
                            Key = "ApiUrl",
                            Value = "https://api.example.com",
                            IsSecret = false,
                            ValueSource = SystemIntegrationParameterSource.Static
                        },
                        new SystemIntegrationParameterRequest
                        {
                            Key = "ApiKey",
                            Value = null,
                            IsSecret = true,
                            ValueSource = SystemIntegrationParameterSource.TenantApiKey,
                            SourceAudience = "integration-platform"
                        }
                    }
                };

                SystemIntegrationResponse created = await service.CreateAsync(request);

                created.Should().NotBeNull();
                created.Id.Should().BeGreaterThan(0);
                created.Name.Should().Be("integration-platform");
                created.BaseUrl.Should().Be("https://integration.example.com");
                created.IsActive.Should().BeTrue();
                created.SystemApplicationId.Should().Be(application.Id);

                IReadOnlyCollection<SystemIntegrationResponse> fetched = await service.GetBySystemApplicationAsync(application.Id);

                fetched.Should().HaveCount(1);

                SystemIntegrationResponse integration = fetched.Single();
                integration.Name.Should().Be("integration-platform");
                integration.BaseUrl.Should().Be("https://integration.example.com");
                integration.IsActive.Should().BeTrue();
                integration.Parameters.Should().HaveCount(2);

                SystemIntegrationParameterResponse staticParam = integration.Parameters
                    .Single(p => p.Key == "ApiUrl");
                staticParam.ValueSource.Should().Be(SystemIntegrationParameterSource.Static);
                staticParam.IsSecret.Should().BeFalse();
                // Parametros nao secretos retornam o valor.
                staticParam.Value.Should().Be("https://api.example.com");
                staticParam.SourceAudience.Should().BeNull();

                SystemIntegrationParameterResponse tenantKeyParam = integration.Parameters
                    .Single(p => p.Key == "ApiKey");
                tenantKeyParam.ValueSource.Should().Be(SystemIntegrationParameterSource.TenantApiKey);
                tenantKeyParam.IsSecret.Should().BeTrue();
                tenantKeyParam.SourceAudience.Should().Be("integration-platform");
                // Parametros secretos sao mascarados: o servico retorna null no Value.
                tenantKeyParam.Value.Should().BeNull();
            });
        }

        [Test]
        public async Task Update_replaces_parameters()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISystemIntegrationService service = sp.GetRequiredService<ISystemIntegrationService>();

                SystemApplication application = await SeedSystemApplicationAsync(dbContext, "IntegrationPlatformUpdate", "integration-platform-aud-update");

                UpsertSystemIntegrationRequest createRequest = new UpsertSystemIntegrationRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "original-name",
                    BaseUrl = "https://original.example.com",
                    IsActive = true,
                    Parameters = new List<SystemIntegrationParameterRequest>
                    {
                        new SystemIntegrationParameterRequest
                        {
                            Key = "OldParam1",
                            Value = "value1",
                            IsSecret = false,
                            ValueSource = SystemIntegrationParameterSource.Static
                        },
                        new SystemIntegrationParameterRequest
                        {
                            Key = "OldParam2",
                            Value = "value2",
                            IsSecret = false,
                            ValueSource = SystemIntegrationParameterSource.Static
                        }
                    }
                };

                SystemIntegrationResponse created = await service.CreateAsync(createRequest);

                UpsertSystemIntegrationRequest updateRequest = new UpsertSystemIntegrationRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "updated-name",
                    BaseUrl = "https://updated.example.com",
                    IsActive = false,
                    Parameters = new List<SystemIntegrationParameterRequest>
                    {
                        new SystemIntegrationParameterRequest
                        {
                            Key = "NewParam",
                            Value = "new-value",
                            IsSecret = false,
                            ValueSource = SystemIntegrationParameterSource.Static
                        }
                    }
                };

                SystemIntegrationResponse updated = await service.UpdateAsync(created.Id, updateRequest);

                updated.Name.Should().Be("updated-name");
                updated.BaseUrl.Should().Be("https://updated.example.com");
                updated.IsActive.Should().BeFalse();
                updated.Parameters.Should().HaveCount(1);
                updated.Parameters.Single().Key.Should().Be("NewParam");
                updated.Parameters.Single().Value.Should().Be("new-value");

                // Confirma via GetBySystemApplicationAsync que os parametros antigos sumiram.
                IReadOnlyCollection<SystemIntegrationResponse> fetched = await service.GetBySystemApplicationAsync(application.Id);
                SystemIntegrationResponse refetched = fetched.Single(i => i.Id == created.Id);
                refetched.Parameters.Should().HaveCount(1);
                refetched.Parameters.Should().NotContain(p => p.Key == "OldParam1");
                refetched.Parameters.Should().NotContain(p => p.Key == "OldParam2");
                refetched.Parameters.Single().Key.Should().Be("NewParam");
            });
        }

        [Test]
        public async Task Delete_removes_integration_and_parameters()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISystemIntegrationService service = sp.GetRequiredService<ISystemIntegrationService>();

                SystemApplication application = await SeedSystemApplicationAsync(dbContext, "IntegrationPlatformDelete", "integration-platform-aud-delete");

                UpsertSystemIntegrationRequest createRequest = new UpsertSystemIntegrationRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "to-be-deleted",
                    BaseUrl = "https://delete.example.com",
                    IsActive = true,
                    Parameters = new List<SystemIntegrationParameterRequest>
                    {
                        new SystemIntegrationParameterRequest
                        {
                            Key = "ParamA",
                            Value = "valueA",
                            IsSecret = false,
                            ValueSource = SystemIntegrationParameterSource.Static
                        },
                        new SystemIntegrationParameterRequest
                        {
                            Key = "ParamB",
                            Value = "valueB",
                            IsSecret = false,
                            ValueSource = SystemIntegrationParameterSource.Static
                        }
                    }
                };

                SystemIntegrationResponse created = await service.CreateAsync(createRequest);
                long integrationId = created.Id;

                await service.DeleteAsync(integrationId);

                IReadOnlyCollection<SystemIntegrationResponse> afterDelete = await service.GetBySystemApplicationAsync(application.Id);
                afterDelete.Should().BeEmpty();

                // Confirma que nao ha parametros orfaos no banco.
                int orphanCount = await dbContext.Set<SystemIntegrationParameter>()
                    .CountAsync(p => p.SystemIntegrationId == integrationId);
                orphanCount.Should().Be(0);
            });
        }

        [Test]
        public async Task Create_with_TenantApiKey_param_without_sourceAudience_is_rejected()
        {
            await InScopeAsync(async sp =>
            {
                DbContext dbContext = sp.GetRequiredService<DbContext>();
                ISystemIntegrationService service = sp.GetRequiredService<ISystemIntegrationService>();

                SystemApplication application = await SeedSystemApplicationAsync(dbContext, "IntegrationPlatformValidation", "integration-platform-aud-validation");

                UpsertSystemIntegrationRequest request = new UpsertSystemIntegrationRequest
                {
                    SystemApplicationId = application.Id,
                    Name = "invalid-integration",
                    BaseUrl = "https://invalid.example.com",
                    IsActive = true,
                    Parameters = new List<SystemIntegrationParameterRequest>
                    {
                        new SystemIntegrationParameterRequest
                        {
                            Key = "ApiKey",
                            Value = null,
                            IsSecret = true,
                            ValueSource = SystemIntegrationParameterSource.TenantApiKey,
                            SourceAudience = null  // invalido: TenantApiKey exige SourceAudience
                        }
                    }
                };

                Func<Task> act = () => service.CreateAsync(request);

                await act.Should().ThrowAsync<ArgumentException>()
                    .WithMessage("*SourceAudience*");
            });
        }
    }
}
