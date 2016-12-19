using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using RService.IO.Abstractions;
using RService.IO.Providers;
using Xunit;

namespace RService.IO.Tests.Providers
{
    public class AuthProviderTests
    {
        private readonly HttpContext _anonymousContext;
        private readonly HttpContext _authorizedContext;

        public AuthProviderTests()
        {
            _anonymousContext = GetContext(s => s.AddTransient<IAuthProvider, AuthProvider>(), true);
            _authorizedContext = GetContext(s => s.AddTransient<IAuthProvider, AuthProvider>());
        }

        [Fact]
        public void InvalidUser()
        {
            var context = GetContext(service => service.AddAuthorization());
            context.User.Identities.Any(x => x.IsAuthenticated).Should().BeTrue();
        }

        [Fact]
        public void Ctor__ThrowsExceptionIfNullProvider()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new AuthProvider(null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("provider");
        }

        [Fact]
        public void IsAuthorizedAsync__ThrowsExceptionIfNullContext()
        {
            var authProvider = GetAuthProvider(_anonymousContext);
            Func<Task> act = async () => await authProvider.IsAuthorizedAsync(null, null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("ctx");
        }

        [Fact]
        public void IsAuthorizedAsync__ThrowsExceptionIfNullFilters()
        {
            var authProvider = GetAuthProvider(_anonymousContext);
            Func<Task> act = async () => await authProvider.IsAuthorizedAsync(_anonymousContext, null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("authorizationFilter");
        }

        [Fact]
        public async void IsAuthorizedAsync__AnonymousReturnsTrueForAnonymous()
        {
            var authProvider = GetAuthProvider(_anonymousContext);
            var attributes = new[] {new AllowAnonymousAttribute()};

            var results = await authProvider.IsAuthorizedAsync(_anonymousContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__AnonymousReturnsFalseForAuthorized()
        {
            var authProvider = GetAuthProvider(_anonymousContext);
            var attributes = new[] {new AuthorizeAttribute()};

            var results = await authProvider.IsAuthorizedAsync(_anonymousContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync__AuthorizedRoleReturnsTrue()
        {
            var authProvider = GetAuthProvider(_authorizedContext);
            var attributes = new[] {new AuthorizeAttribute {Roles = "Administrator"}};

            var results = await authProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__UnauthorizedRoleReturnsFalse()
        {
            var authProvider = GetAuthProvider(_authorizedContext);
            var attributes = new[] {new AuthorizeAttribute {Roles = "Poweruser"}};

            var results = await authProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorized__AuthoriedWithNoAttributes()
        {
            var authProvider = GetAuthProvider(_authorizedContext);
            var attributes = new object[] {};

            var results = await authProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorized__AuthorizedWithSpecifiedScheme()
        {
            var authProvider = GetAuthProvider(_authorizedContext);
            var attributes = new[]
            {
                new AuthorizeAttribute
                {
                    Roles = "Administrator",
                    ActiveAuthenticationSchemes = "Basic"
                }
            };

            var results = await authProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorized__NotAuthorizedWithSpecifiedMissingScheme()
        {
            var authProvider = GetAuthProvider(_authorizedContext);
            var attributes = new[]
            {
                new AuthorizeAttribute
                {
                    Roles = "Administrator",
                    ActiveAuthenticationSchemes = "FizzBuzzScheme"
                }
            };

            var results = await authProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorized__ProvideDefaultIdentityIfClaimsFails()
        {
            var authProvider = GetAuthProvider(_anonymousContext);
            var attributes = new[]
            {
                new AuthorizeAttribute
                {
                    Roles = "Administrator",
                    ActiveAuthenticationSchemes = "Basic"
                }
            };

            _anonymousContext.User.Should().BeNull();
            await authProvider.IsAuthorizedAsync(_anonymousContext, attributes);
            _anonymousContext.User.Should().NotBeNull();
        }

        private static HttpContext GetContext(Action<ServiceCollection> registerServices, bool anonymous = false)
        {
            var basicPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                    {
                        new Claim("Permission", "CanViewPage"),
                        new Claim(ClaimTypes.Role, "Administrator"),
                        new Claim(ClaimTypes.Role, "User"),
                        new Claim(ClaimTypes.NameIdentifier, "John"),
                    },
                    "Basic"
                ));

            var validUser = basicPrincipal;

            var bearerIdentity = new ClaimsIdentity(new[]
                {
                    new Claim("Permission", "CupBearer"),
                    new Claim(ClaimTypes.Role, "Token"),
                    new Claim(ClaimTypes.NameIdentifier, "Jon Bear")
                },
                "Bearer"
            );
            var bearerPrincipal = new ClaimsPrincipal(bearerIdentity);

            validUser.AddIdentity(bearerIdentity);

            var serviceCollection = new ServiceCollection();
            if (registerServices != null)
            {
                serviceCollection.AddOptions();
                serviceCollection.AddLogging();
                serviceCollection.AddAuthorization();

                registerServices.Invoke(serviceCollection);
            }

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var context = new Mock<HttpContext>();
            var auth = new Mock<AuthenticationManager>();
            context.Setup(x => x.Authentication).Returns(auth.Object);
            context.SetupProperty(x => x.User);
            if (!anonymous)
                context.Object.User = validUser;
            context.SetupGet(x => x.RequestServices).Returns(serviceProvider);
            auth.Setup(x => x.AuthenticateAsync("Bearer")).ReturnsAsync(bearerPrincipal);
            auth.Setup(x => x.AuthenticateAsync("Basic")).ReturnsAsync(basicPrincipal);
            auth.Setup(x => x.AuthenticateAsync("Fails")).ReturnsAsync(null);

            return context.Object;
        }

        private static IAuthProvider GetAuthProvider(HttpContext ctx)
        {
            return ctx.RequestServices.GetRequiredService<IAuthProvider>();
        }
    }
}