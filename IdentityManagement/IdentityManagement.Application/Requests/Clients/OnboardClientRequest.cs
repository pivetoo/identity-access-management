using System.ComponentModel.DataAnnotations;

namespace IdentityManagement.Application.Requests.Clients
{
    public sealed class OnboardClientRequest
    {
        [Required] public string LegalName { get; set; } = string.Empty;
        [Required] public string TradeName { get; set; } = string.Empty;
        [Required] public string Document { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        [Required][MinLength(1)] public List<OnboardClientSystemItem> Systems { get; set; } = new();
    }

    public sealed class OnboardClientSystemItem
    {
        [Required][Range(1, long.MaxValue)] public long SystemApplicationId { get; set; }
        [Required] public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
    }
}
