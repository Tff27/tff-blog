using AwesomeAssertions;
using Tff.Blog.Api.Responses;

namespace Tff.Blog.Api.Unit.Tests.Responses;

public class ConditionalResponseTests
{
    [Fact]
    public void CreateETag_QuotesALowercaseSha256Hash()
    {
        var etag = ConditionalResponse.CreateETag("<root />");

        etag.Should().MatchRegex("^\"[0-9a-f]{64}\"$");
    }

    [Fact]
    public void CreateETag_IsStableForTheSamePayload()
    {
        ConditionalResponse.CreateETag("<root />")
            .Should()
            .Be(ConditionalResponse.CreateETag("<root />"));
    }

    [Fact]
    public void CreateETag_ChangesWithThePayload()
    {
        ConditionalResponse.CreateETag("<root one=\"1\" />")
            .Should()
            .NotBe(ConditionalResponse.CreateETag("<root one=\"2\" />"));
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
        ConditionalResponse.MatchesETag(header, etag).Should().Be(expected);
    }
}
