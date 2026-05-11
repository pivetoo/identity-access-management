namespace IdentityManagement.Application.Requests.Auth
{
    public sealed record ResetPasswordRequest(string Token, string NewPassword);
}
