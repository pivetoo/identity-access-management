using IdentityManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace IdentityManagement.Testing.Infrastructure.Services
{
    [TestFixture]
    public class JwtServiceTests
    {
        private IConfiguration configuration = null!;

        [SetUp]
        public void SetUp()
        {
            configuration = Substitute.For<IConfiguration>();
            configuration["Jwt:Issuer"].Returns("test-issuer");
        }

        [Test]
        public void GenerateRefreshToken_ShouldReturnNonEmptyString()
        {
            var jwtService = CreateJwtService();

            string token = jwtService.GenerateRefreshToken();

            Assert.That(token, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void GenerateRefreshToken_ShouldReturnDifferentTokensForDifferentCalls()
        {
            var jwtService = CreateJwtService();

            string token1 = jwtService.GenerateRefreshToken();
            string token2 = jwtService.GenerateRefreshToken();

            Assert.That(token1, Is.Not.EqualTo(token2));
        }

        [Test]
        public void GenerateRefreshToken_ShouldReturnBase64String()
        {
            var jwtService = CreateJwtService();

            string token = jwtService.GenerateRefreshToken();

            byte[] bytes = Convert.FromBase64String(token);
            Assert.That(bytes.Length, Is.EqualTo(64));
        }

        private JwtService CreateJwtService()
        {
            var dbContext = Substitute.For<Microsoft.EntityFrameworkCore.DbContext>();
            return new JwtService(dbContext, configuration);
        }
    }
}
