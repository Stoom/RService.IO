using System;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace RService.IO.Tests
{
    public class RServiceOptionsTests
    {
        private readonly RServiceOptions _options = new RServiceOptions();

        [Fact]
        public void AddServiceAsssembly__AddsAssemblyFromType()
        {
            var type = typeof(SvcWithMethodRoute);
            var asm = type.GetTypeInfo().Assembly;

            _options.AddServiceAssembly(type);

            _options.ServiceAssemblies.Should().Contain(asm);
        }

        [Fact]
        public void ServiceAssemblies__DefaultsToEmptyList()
        {

            _options.ServiceAssemblies.Should().NotBeNull();
            _options.ServiceAssemblies.Count.Should().Be(0);
        }

        [Fact]
        public void AddServiceAssembly__ThrowsExceptionOnNullType()
        {
            Action comparison = () => _options.AddServiceAssembly(null);
            comparison.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void EnableDebugging__DefaultsToFalse()
        {
            _options.EnableDebugging.Should().BeFalse();
        }

        [Fact]
        public void SerializationProviders__DefaultsToEmptyDictionary()
        {
            _options.SerializationProviders.Should().NotBeNull()
                .And.Subject.Count.ShouldBeEquivalentTo(0);
        }

        [Fact]
        public void DefaultSerializationProvider__DefaultsToNull()
        {
            _options.DefaultSerializationProvider.Should().BeNull();
        }
    }
}