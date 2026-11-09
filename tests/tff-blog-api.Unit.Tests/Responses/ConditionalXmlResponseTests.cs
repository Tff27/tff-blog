using AwesomeAssertions;
using Tff.Blog.Api.Responses;

namespace Tff.Blog.Api.Unit.Tests.Responses;

public class ConditionalXmlResponseTests
{
    [Fact]
    public void CreateETag_QuotesALowercaseSha256Hash()
    {
        var etag = ConditionalXmlResponse.CreateETag("<root />");

        etag.Should().MatchRegex("^\"[0-9a-f]{64}\"$");
    }

    [Fact]
    public void CreateETag_IsStableForTheSamePayload()
    {
        ConditionalXmlResponse.CreateETag("<root />")
            .Should()
            .Be(ConditionalXmlResponse.CreateETag("<root />"));
    }

    [Fact]
    public void CreateETag_ChangesWithThePayload()
    {
        ConditionalXmlResponse.CreateETag("<root one=\"1\" />")
            .Should()
            .NotBe(ConditionalXmlResponse.CreateETag("<root one=\"2\" />"));
    }

    [Theory]
    [InlineData("\"etag\"", "\"etag\"", true)]
    [InlineData("W/\"etag\"", "\"etag\"", true)]
    [InlineData("\"other\", \"etag\"", "\"etag\"", true)]
    [InlineData("*", "\"etag\"", true)]
    [InlineData("\"other\"", "\"etag\"", false)]
    [InlineData("", "\"etag\"", false)]
    [InlineData(null, "\"etag\"", false)]
    public void MatchesETag_ComparesTheIfNoneMatchHeader(string? header, string etag, bool expected)
    {
        ConditionalXmlResponse.MatchesETag(header, etag).Should().Be(expected);
    }
}
