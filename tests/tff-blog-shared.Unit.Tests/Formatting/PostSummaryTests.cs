using AwesomeAssertions;
using Tff.Blog.Shared.Formatting;

namespace Tff.Blog.Shared.Unit.Tests.Formatting;

public class PostSummaryTests
{
    [Fact]
    public void ForSocialShare_LeavesShortDescriptionsUntouched()
    {
        PostSummary.ForSocialShare("A short description").Should().Be("A short description");
    }

    [Fact]
    public void ForSocialShare_TrimsSurroundingWhitespace()
    {
        PostSummary.ForSocialShare("  A short description  ").Should().Be("A short description");
    }

    [Fact]
    public void ForSocialShare_TruncatesAtAWordBoundaryWithAnEllipsis()
    {
        var description = new string('a', 190) + " supercalifragilistic tail";

        PostSummary.ForSocialShare(description).Should().Be(new string('a', 190) + "…");
    }

    [Fact]
    public void ForSocialShare_CapsTheLengthWhenThereIsNoWordBoundary()
    {
        PostSummary.ForSocialShare(new string('a', 250)).Should().Be(new string('a', 200) + "…");
    }

    [Fact]
    public void ForSocialShare_TrimsTrailingPunctuationBeforeTheEllipsis()
    {
        var description = new string('a', 196) + ", more text after the limit";

        PostSummary.ForSocialShare(description).Should().Be(new string('a', 196) + "…");
    }

    [Fact]
    public void ForSocialShare_LeavesTextAtTheLimitUnchanged()
    {
        var description = new string('a', PostSummary.SocialDescriptionMaxLength);

        PostSummary.ForSocialShare(description).Should().Be(description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ForSocialShare_WithoutADescription_ReturnsEmpty(string? description)
    {
        PostSummary.ForSocialShare(description).Should().BeEmpty();
    }
}
