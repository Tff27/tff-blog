using AwesomeAssertions;
using Tff.Blog.Api.Shell;

namespace Tff.Blog.Api.Unit.Tests.Shell;

public class LinkUnfurlersTests
{
    [Theory]
    [InlineData("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
    [InlineData("Twitterbot/1.0")]
    [InlineData("LinkedInBot/1.0 (compatible; Mozilla/5.0)")]
    [InlineData("Slackbot-LinkExpanding 1.0 (+https://api.slack.com/robots)")]
    [InlineData("Discordbot/2.0; +https://discordapp.com")]
    [InlineData("WhatsApp/2.23.20.0")]
    [InlineData("TelegramBot (like TwitterBot)")]
    public void IsMatch_RecognisesLinkUnfurlers(string userAgent)
    {
        LinkUnfurlers.IsMatch(userAgent).Should().BeTrue();
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
    [InlineData("curl/8.7.1")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsMatch_IgnoresBrowsersSearchEnginesAndMissingAgents(string? userAgent)
    {
        LinkUnfurlers.IsMatch(userAgent).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_IgnoresCase()
    {
        LinkUnfurlers.IsMatch("fAcEbOoKeXtErNaLhIt/1.1").Should().BeTrue();
    }
}
