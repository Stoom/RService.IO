using FluentAssertions;
using RService.IO.Abstractions;
using Xunit;

namespace RService.IO.Tests.Abstractions
{
    public class RestVerbsTests
    {
        [Fact]
        public void ToEnumerable__ReturnsAnyIfVerbIsAny()
        {
            var expected = new[] { "ANY" };
            var actual = RestVerbs.Any.ToEnumerable();

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ToEnumerable__ReturnsGetIfVerbIsGet()
        {
            var expected = new[] { "GET" };
            var actual = RestVerbs.Get.ToEnumerable();

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void ToEnumerable__ReturnsGetAndPostIfVerbsAreGetAndPost()
        {
            var expected = new[] { "GET", "POST" };
            var actual = (RestVerbs.Get|RestVerbs.Post).ToEnumerable();

            actual.Should().BeEquivalentTo(expected);
        }
    }
}