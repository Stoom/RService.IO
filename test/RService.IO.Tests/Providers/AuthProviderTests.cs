using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using RService.IO.Abstractions;
using RService.IO.Abstractions.Providers;
using RService.IO.Providers;
using Xunit;

namespace RService.IO.Tests.Providers
{
    public class AuthProviderTests
    {
        private readonly HttpContext _anonymousContext;
        private readonly HttpContext _authorizedContext;
        private readonly IAuthProvider _anonymousAuthProvider;
        private readonly IAuthProvider _authorizedAuthProvider;

        public AuthProviderTests()
        {
            _anonymousContext = GetContext(s => s.AddTransient<IAuthProvider, AuthProvider>(), true);
            _authorizedContext = GetContext(s => s.AddTransient<IAuthProvider, AuthProvider>());
            _anonymousAuthProvider = GetAuthProvider(_anonymousContext);
            _authorizedAuthProvider = GetAuthProvider(_authorizedContext);
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
        public void IsAuthorizedAsync_Filter__ThrowsExceptionIfNullContext()
        {
            Func<Task> act = async () => await _authorizedAuthProvider.IsAuthorizedAsync(null, (IEnumerable<object>)null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("ctx");
        }

        [Fact]
        public void IsAuthorizedAsync_Filter__ThrowsExceptionIfNullFilters()
        {
            Func<Task> act = async () => await _anonymousAuthProvider.IsAuthorizedAsync(_anonymousContext, (IEnumerable<object>)null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("authorizationFilter");
        }

        [Fact]
        public void IsAuthorizedAsync_Metadata__ThrowsExceptionIfNullContext()
        {
            Func<Task> act = async () => await _anonymousAuthProvider.IsAuthorizedAsync(null, (ServiceMetadata)null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("ctx");
        }

        [Fact]
        public void IsAuthorizedAsync_Metadata__ThrowsExceptionIfNullMetadata()
        {
            Func<Task> act = async () => await _anonymousAuthProvider.IsAuthorizedAsync(_anonymousContext, (ServiceMetadata)null);

            act.ShouldThrow<ArgumentNullException>().And.Message.Should().Contain("metadata");
        }

        [Fact]
        public async void IsAuthorizedAsync__AnonymousReturnsTrueForAnonymous()
        {
            var attributes = new[] { new AllowAnonymousAttribute() };

            var results = await _anonymousAuthProvider.IsAuthorizedAsync(_anonymousContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__AnonymousReturnsFalseForAuthorized()
        {
            var attributes = new[] { new AuthorizeAttribute() };

            var results = await _anonymousAuthProvider.IsAuthorizedAsync(_anonymousContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync__AuthorizedRoleReturnsTrue()
        {
            var attributes = new[] { new AuthorizeAttribute { Roles = "Administrator" } };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__UnauthorizedRoleReturnsFalse()
        {
            var authProvider = GetAuthProvider(_authorizedContext);
            var attributes = new[] { new AuthorizeAttribute { Roles = "Poweruser" } };

            var results = await authProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync__AuthoriedWithNoAttributes()
        {
            var attributes = new object[] { };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__AuthorizedWithSpecifiedScheme()
        {
            var attributes = new[]
            {
                new AuthorizeAttribute
                {
                    Roles = "Administrator",
                    ActiveAuthenticationSchemes = "Basic"
                }
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__NotAuthorizedWithSpecifiedMissingScheme()
        {
            var attributes = new[]
            {
                new AuthorizeAttribute
                {
                    Roles = "Administrator",
                    ActiveAuthenticationSchemes = "FizzBuzzScheme"
                }
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync__ProvideDefaultIdentityIfClaimsFails()
        {
            var attributes = new[]
            {
                new AuthorizeAttribute
                {
                    Roles = "Administrator",
                    ActiveAuthenticationSchemes = "Basic"
                }
            };

            _anonymousContext.User.Should().BeNull();
            await _anonymousAuthProvider.IsAuthorizedAsync(_anonymousContext, attributes);
            _anonymousContext.User.Should().NotBeNull();
        }

        [Fact]
        public async void IsAuthorizedAsync__AuthorizedWhenSpecifingMultipleRolesInSingleAttr()
        {
            var attributes = new[] { new AuthorizeAttribute { Roles = "Administrator, PowerUser" } };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync__AuthorizedRequireAllAttrToBeAuthorized()
        {
            var attributes = new[]
            {
                new AuthorizeAttribute { Roles = "Administrator" },
                new AuthorizeAttribute { Roles = "PowerUser" }
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, attributes);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesSingleSuccessfulMethod()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(ServiceMetadata).GetTypeInfo(),
                Method = typeof(MethodAuth).GetPublicMethods().Single(x => x.Name == "Single")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesSingleFailedMethod()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(ServiceMetadata).GetTypeInfo(),
                Method = typeof(MethodAuth).GetPublicMethods().Single(x => x.Name == "SingleFail")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesMultipleSuccessfulMethod()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(ServiceMetadata).GetTypeInfo(),
                Method = typeof(MethodAuth).GetPublicMethods().Single(x => x.Name == "Multiple")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesMultipleFailedMethod()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(ServiceMetadata).GetTypeInfo(),
                Method = typeof(MethodAuth).GetPublicMethods().Single(x => x.Name == "MultipleFail")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesSingleSuccessfulClass()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(SingleClassAuth).GetTypeInfo(),
                Method = typeof(SingleClassAuth).GetPublicMethods().Single(x => x.Name == "Any")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesSingleFailedClass()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(SingleFailClassAuth).GetTypeInfo(),
                Method = typeof(SingleFailClassAuth).GetPublicMethods().Single(x => x.Name == "Any")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesMultipleSuccessfulClass()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(MultipleClassAuth).GetTypeInfo(),
                Method = typeof(MultipleClassAuth).GetPublicMethods().Single(x => x.Name == "Any")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesMultipleFailedClass()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(MultipleFailClassAuth).GetTypeInfo(),
                Method = typeof(MultipleFailClassAuth).GetPublicMethods().Single(x => x.Name == "Any")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesSuccessfulHybrid()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(HybridAuth).GetTypeInfo(),
                Method = typeof(HybridAuth).GetPublicMethods().Single(x => x.Name == "Any")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesMethodFailedHybrid()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(HybridAuth).GetTypeInfo(),
                Method = typeof(HybridAuth).GetPublicMethods().Single(x => x.Name == "AnyFail")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesClassFailedHybrid()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(HybridFailAuth).GetTypeInfo(),
                Method = typeof(HybridFailAuth).GetPublicMethods().Single(x => x.Name == "Fail")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeFalse();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesAnonymousMethodSuccessfulHybrid()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(HybridFailAuth).GetTypeInfo(),
                Method = typeof(HybridFailAuth).GetPublicMethods().Single(x => x.Name == "Anonymous")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
        }

        [Fact]
        public async void IsAuthorizedAsync_Metadata__HandlesAnonymousClassSuccessfulHybrid()
        {
            var metadata = new ServiceMetadata
            {
                Ident = Guid.NewGuid().ToString(),
                Service = typeof(HybridAnonymousFailAuth).GetTypeInfo(),
                Method = typeof(HybridAnonymousFailAuth).GetPublicMethods().Single(x => x.Name == "Anonymous")
            };

            var results = await _authorizedAuthProvider.IsAuthorizedAsync(_authorizedContext, metadata);
            results.Should().BeTrue();
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

        #region  Test Classes
        // ReSharper disable UnusedMember.Local
        private class MethodAuth
        {
            [Authorize(Roles = "Administrator")]
            public object Single()
            {
                return null;
            }
            [Authorize(Roles = "FakeRole")]
            public object SingleFail()
            {
                return null;
            }

            [Authorize(Roles = "Administrator")]
            [Authorize(Roles = "User")]
            public object Multiple()
            {
                return null;
            }
            [Authorize(Roles = "Administrator")]
            [Authorize(Roles = "FakeRole")]
            public object MultipleFail()
            {
                return null;
            }
        }

        [Authorize(Roles = "Administrator")]
        private class SingleClassAuth
        {
            public object Any()
            {
                return null;
            }
        }

        [Authorize(Roles = "FakeRole")]
        private class SingleFailClassAuth
        {
            public object Any()
            {
                return null;
            }
        }

        [Authorize(Roles = "Administrator")]
        [Authorize(Roles = "User")]
        private class MultipleClassAuth
        {
            public object Any()
            {
                return null;
            }
        }

        [Authorize(Roles = "Administrator")]
        [Authorize(Roles = "FakeRole")]
        private class MultipleFailClassAuth
        {
            public object Any()
            {
                return null;
            }
        }

        [Authorize(Roles = "Administrator")]
        private class HybridAuth
        {
            [Authorize(Roles = "User")]
            public object Any()
            {
                return null;
            }

            [Authorize(Roles = "FakeRole")]
            public object AnyFail()
            {
                return null;
            }
        }

        [Authorize(Roles = "FakeRole")]
        private class HybridFailAuth
        {
            [AllowAnonymous]
            public object Anonymous()
            {
                return null;
            }

            [Authorize(Roles = "Administrator")]
            public object Fail()
            {
                return null;
            }
        }

        [AllowAnonymous]
        private class HybridAnonymousFailAuth
        {
            [Authorize(Roles = "FakeRole")]
            public object Anonymous()
            {
                return null;
            }
        }
        // ReSharper restore UnusedMember.Local
        #endregion
    }
}