using System.ComponentModel.DataAnnotations;
using IdentityManagement.Domain.ValueObjects;

namespace IdentityManagement.Application.Requests.SystemApplications
{
    public class UpdateSystemApplicationRequest
    {
        [Required]
        public long Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string RedirectUris { get; set; } = string.Empty;

        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Audience { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public ApplicationType Type { get; set; } = ApplicationType.External;
    }
}
