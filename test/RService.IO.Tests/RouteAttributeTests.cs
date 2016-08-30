using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests
{
    public class RouteAttributeTests
    {
        private const string FIRST_PATH = "/foobar";
        private const string SECOND_PATH = "/llama";

        [Fact]
        public void DecorateClassWithPath()
        {
            var attrs = typeof(AttrPath).GetAttributes<RouteAttribute>().ToList();

            Assert.Equal(1, attrs.Count);
        }

        [Fact]
        public void DecorateClassWithPathAndVerb()
        {
            var attrs = typeof(AttrPathVerb).GetAttributes<RouteAttribute>().ToList();

            Assert.Equal(1, attrs.Count);
        }

        [Fact]
        public void AllowsMultipleAttributes()
        {
            var attrs = typeof(AttrMulti).GetAttributes<RouteAttribute>().ToList();

            Assert.Equal(2, attrs.Count);
        }

        [Fact]
        public void DecorateMethodWithPath()
        {
            var methods =
                typeof(AttrMethodPath).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttributes<RouteAttribute>().Any())
                    .ToList();

            Assert.Equal(1, methods.Count);
        }

        [Fact]
        public void DecorateMethodWithPatAndVerbh()
        {
            var methods =
                typeof(AttrMethodPathVerb).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(m => m.GetCustomAttributes<RouteAttribute>().Any())
                    .ToList();

            Assert.Equal(1, methods.Count);
        }

        [Fact]
        public void Ctor__ThrowsExcetpionWithNullPath()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action comparison = () => new RouteAttribute(null);
            comparison.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Ctor__ThrowsExcetpionWithBlankPath()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action comparison = () => new RouteAttribute(string.Empty);
            comparison.ShouldThrow<ArgumentException>();
        }

        #region Classes to test attribute
        [Route(FIRST_PATH)]
        private class AttrPath
        {
        }

        [Route(FIRST_PATH, RestVerbs.Get)]
        private class AttrPathVerb
        {
        }

        [Route(FIRST_PATH)]
        [Route(SECOND_PATH)]
        private class AttrMulti
        {
        }

        private class AttrMethodPath
        {
            [Route(FIRST_PATH)]
            // ReSharper disable once MemberHidesStaticFromOuterClass
            // ReSharper disable once UnusedMember.Local
            internal void AttrPath()
            {
            }
        }

        private class AttrMethodPathVerb
        {
            [Route(FIRST_PATH, RestVerbs.Get)]
            // ReSharper disable once MemberHidesStaticFromOuterClass
            // ReSharper disable once UnusedMember.Local
            internal void AttrPathVerb()
            {
            }
        }
        #endregion
    }
}
