using System.Reflection;
using Archon.Api.Attributes;
using Archon.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace IdentityManagement.Testing.Api
{
    /// <summary>
    /// Este e o teste que o `FallbackPolicy` do <c>Program.cs</c> nao consegue dar sozinho: ele
    /// falha em tempo de CI quando alguem adiciona um endpoint sem decidir se ele e protegido ou
    /// anonimo. Antes disso, endpoint sem marcacao nascia publico em silencio — foi assim que
    /// `AccessResources/Sync` aceitou POST anonimo.
    /// </summary>
    [TestFixture]
    public class EndpointAuthorizationTests
    {
        private static IEnumerable<MethodInfo> ActionMethods()
        {
            // O Archon registra controllers proprios (Localization, Health, Audit...) nas rotas deste
            // app, e o FallbackPolicy vale para eles igual. Varrer so o assembly do IdM deixou o
            // catalogo de localizacao sem marcacao: ele passou a devolver 401 e a tela de login
            // entrou em loop, porque consome o catalogo antes de existir sessao.
            Assembly[] apiAssemblies =
            [
                typeof(IdentityManagement.Api.RateLimitPolicies).Assembly,
                typeof(ApiControllerBase).Assembly
            ];

            return apiAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(ApiControllerBase)) && !type.IsAbstract)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(method => !method.IsSpecialName && method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any());
        }

        private static bool IsDecided(MethodInfo method)
        {
            bool anonymous = method.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null ||
                             method.DeclaringType!.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;

            bool guarded = method.GetCustomAttribute<RequireAccessAttribute>(inherit: true) is not null ||
                           method.DeclaringType!.GetCustomAttribute<RequireAccessAttribute>(inherit: true) is not null ||
                           method.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is not null ||
                           method.DeclaringType!.GetCustomAttribute<AuthorizeAttribute>(inherit: true) is not null;

            return anonymous || guarded;
        }

        [Test]
        public void EveryEndpoint_ShouldDeclareAuthorizationExplicitly()
        {
            List<string> undecided = ActionMethods()
                .Where(method => !IsDecided(method))
                .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
                .OrderBy(name => name)
                .ToList();

            Assert.That(
                undecided,
                Is.Empty,
                $"Endpoint sem [RequireAccess], [Authorize] ou [AllowAnonymous]: {string.Join(", ", undecided)}. " +
                "Marque explicitamente — o padrao do pipeline nao pode ser a unica barreira.");
        }

        [Test]
        public void ActionMethods_ShouldBeDiscovered()
        {
            // Guarda contra o teste acima passar por nao ter encontrado nada.
            Assert.That(ActionMethods().Count(), Is.GreaterThan(50));
        }
    }
}
