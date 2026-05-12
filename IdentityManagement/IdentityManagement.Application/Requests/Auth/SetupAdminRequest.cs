namespace IdentityManagement.Application.Requests.Auth
{
    public sealed record SetupAdminRequest(string Token, string Name, string Username, string Email, string Password);
}
