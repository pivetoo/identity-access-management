using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.SystemIntegrations
{
    public class UpsertSystemIntegrationRequest
    {
        [Required]
        [Range(1, long.MaxValue)]
        public long SystemApplicationId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string BaseUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<SystemIntegrationParameterRequest> Parameters { get; set; } = new();
    }
}
