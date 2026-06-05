namespace IdentityManagement.Application.Requests.Auth
{
    public sealed record SetupAdminExistingUserRequest(string Token, string UsernameOrEmail, string Password);
}
