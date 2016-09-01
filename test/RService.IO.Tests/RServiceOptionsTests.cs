using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace RService.IO.Tests
{
    public class RServiceOptionsTests
    {
        [Fact]
        public void AddServiceAsssembly__AddsAssemblyFromType()
        {
            var type = typeof(SvcWithMethodRoute);
            var asm = type.GetTypeInfo().Assembly;
            var options = new RServiceOptions();
            
            options.AddServiceAssembly(type);

            options.ServiceAssemblies.Should().Contain(asm);
        }

        [Fact]
        public void RouteHandler__DefaultsToRServiceHandler()
        {
            var options = new RServiceOptions();
            var expected = (RequestDelegate) Handler.ServiceHandler;

            options.RouteHanlder.Should().Be(expected);
        }

        [Fact]
        public void ServiceAssemblies__DefaultsToEmptyList()
        {
            var options = new RServiceOptions();

            options.ServiceAssemblies.Should().NotBeNull();
            options.ServiceAssemblies.Count.Should().Be(0);
        }

        [Fact]
        public void AddServiceAssembly__ThrowsExceptionOnNullType()
        {
            var options = new RServiceOptions();
            Action comparison = () => options.AddServiceAssembly(null);
            comparison.ShouldThrow<ArgumentNullException>();
        }
    }
}