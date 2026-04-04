using Archon.Api.Attributes;
using Archon.Api.Controllers;
using IdentityManagement.Application.Requests.SystemRoleTemplates;
using IdentityManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManagement.Api.Controllers
{
    public sealed class SystemRoleTemplatesController : ApiControllerBase
    {
        private readonly ISystemRoleTemplateService systemRoleTemplateService;

        public SystemRoleTemplatesController(ISystemRoleTemplateService systemRoleTemplateService)
        {
            this.systemRoleTemplateService = systemRoleTemplateService;
        }

        [RequireAccess("Permite cadastrar um perfil padrão para uma aplicação do sistema.")]
        [PostEndpoint("")]
        public async Task<IActionResult> Create([FromBody] CreateSystemRoleTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await systemRoleTemplateService.CreateSystemRoleTemplate(request, cancellationToken);
            return Http201(response, "System role template created successfully.");
        }

        [RequireAccess("Permite atualizar um perfil padrão de aplicação.")]
        [PutEndpoint("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateSystemRoleTemplateRequest request, CancellationToken cancellationToken)
        {
            IActionResult? validationResult = ValidateBody(request);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var response = await systemRoleTemplateService.UpdateSystemRoleTemplate(id, request, cancellationToken);
            return Http200(response, "System role template updated successfully.");
        }

        [RequireAccess("Permite listar os perfis padrão de uma aplicação específica.")]
        [GetEndpoint("system-application/{systemApplicationId:long}")]
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
            return response is null ? Http404("System role template not found.") : Http200(response);
        }
    }
}
