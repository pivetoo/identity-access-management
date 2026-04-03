namespace IdentityManagement.Application.Services
{
    public interface ITemporaryTokenService
    {
        string GenerateTemporaryToken(long userId);

        bool ValidateTemporaryToken(string token, out long userId);

        bool ValidateTemporaryTokenForUser(string token, long userId);
    }
}
