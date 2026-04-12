using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Localization;
using IdentityManagement.Application.Requests.SystemRoleTemplates;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdentityManagement.Api.Controllers
{
    public sealed class SystemRoleTemplatesController : ApiControllerBase
    {
        private readonly ISystemRoleTemplateService systemRoleTemplateService;
        private new readonly IStringLocalizer<IdentityManagementResource> Localizer;

        public SystemRoleTemplatesController(ISystemRoleTemplateService systemRoleTemplateService, IStringLocalizer<IdentityManagementResource> Localizer)
        {
            this.systemRoleTemplateService = systemRoleTemplateService;
            this.Localizer = Localizer;
        }

        [RequireAccess("Permite listar os perfis padrão de uma aplicação específica.")]
        [GetEndpoint("{systemApplicationId:long}")]
        public async Task<IActionResult> GetBySystemApplicationId(long systemApplicationId, CancellationToken cancellationToken)
        {
            var response = await systemRoleTemplateService.GetBySystemApplicationId(systemApplicationId, cancellationToken);
            return Http200(response);
        }

        [RequireAccess("Permite consultar os detalhes de um perfil padrão de aplicação.")]
        [GetEndpoint("{id:long}")]
        public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
        {
            var response = await systemRoleTemplateService.GetById(id, cancellationToken);
            return response is null ? Http404(Localizer["systemRoleTemplate.notFound"]) : Http200(response);
        }

        [RequireAccess("Permite cadastrar um perfil padrão para uma aplicação do sistema.")]
        [PostEndpoint]
        public async Task<IActionResult> Create([FromBody] CreateSystemRoleTemplateRequest request, CancellationToken cancellationToken)
        {
            var response = await systemRoleTemplateService.CreateSystemRoleTemplate(request, cancellationToken);
            return Http201(response, Localizer["systemRoleTemplate.created"]);
        }

        [RequireAccess("Permite atualizar um perfil padrão de aplicação.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSystemRoleTemplateRequest request, CancellationToken cancellationToken)
        {
            var response = await systemRoleTemplateService.UpdateSystemRoleTemplate(id, request, cancellationToken);
            return Http200(response, Localizer["systemRoleTemplate.updated"]);
        }
    }
}
