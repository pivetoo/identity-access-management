using Archon.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IdentityManagement.Api.Attributes
{
    public sealed class RequireIntegrationSecretAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            IConfiguration configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            string? expectedSecret = configuration["IntegrationSecret"];

            if (string.IsNullOrWhiteSpace(expectedSecret))
            {
                context.Result = new ObjectResult(new ApiResponse
                {
                    Message = "IntegrationSecret is not configured."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };

                return;
            }

            string? providedSecret = context.HttpContext.Request.Headers["X-Integration-Secret"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(providedSecret))
            {
                context.Result = new ObjectResult(new ApiResponse
                {
                    Message = "X-Integration-Secret header is required."
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };

                return;
            }

            if (!string.Equals(expectedSecret, providedSecret, StringComparison.Ordinal))
            {
                context.Result = new ObjectResult(new ApiResponse
                {
                    Message = "Invalid X-Integration-Secret."
                })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };

                return;
            }
        }
    }
}
