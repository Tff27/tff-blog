using AwesomeAssertions;
using Tff.Blog.Api.Configuration;

namespace Tff.Blog.Api.Unit.Tests.Configuration;

public class SettingsTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData(" TRUE ", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("yes", false)]
    public void ParseBoolean_ReturnsExpectedValue(string? value, bool expected)
    {
        Settings.ParseBoolean(value).Should().Be(expected);
    }

    [Fact]
    public void ParseBoolean_WhenNull_ReturnsFalse()
    {
        Settings.ParseBoolean(null).Should().BeFalse();
    }
}
