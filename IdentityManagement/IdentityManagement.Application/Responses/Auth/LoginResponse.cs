using IdentityManagement.Application.Responses.Contracts;
using IdentityManagement.Application.Responses.Users;

namespace IdentityManagement.Application.Responses.Auth
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = string.Empty;

        public int ExpiresIn { get; set; }

        public string? RedirectUrl { get; set; }

        public UserResponse User { get; set; } = new();

        public ContractSelectionResponseItem Contract { get; set; } = new();
    }
}
