using AwesomeAssertions;
using Bunit;
using Tff.Blog.Client.Shared.Molecules;

namespace Tff.Blog.Client.Unit.Tests.Shared.Molecules;

public class SocialShareTests : BunitContext
{
    private const string Url = "https://example.com/post";
    private const string Text = "Hello world";

    [Fact]
    public void SocialShare_RendersOneLinkPerNetwork()
    {
        var cut = CreateComponent();

        cut.FindAll("a").Should().HaveCount(4);
    }

    [Fact]
    public void SocialShare_FacebookLinkContainsTheUrl()
    {
        var cut = CreateComponent();

        cut.Find("a[href^='https://www.facebook.com/sharer']").GetAttribute("href")
            .Should().Be($"https://www.facebook.com/sharer/sharer.php?u={Url}");
    }

    [Fact]
    public void SocialShare_LinkedInLinkContainsTheUrl()
    {
        var cut = CreateComponent();

        cut.Find("a[href^='https://www.linkedin.com/sharing']").GetAttribute("href")
            .Should().Be($"https://www.linkedin.com/sharing/share-offsite/?url={Url}");
    }

    [Fact]
    public void SocialShare_TwitterLinkUrlEncodesTheText()
    {
        var cut = CreateComponent();

        cut.Find("a[href^='https://twitter.com/intent/tweet']").GetAttribute("href")
            .Should().Be($"https://twitter.com/intent/tweet?url={Url}&text=Hello+world");
    }

    [Fact]
    public void SocialShare_MailLinkContainsSubjectAndBody()
    {
        var cut = CreateComponent();

        cut.Find("a[href^='mailto:']").GetAttribute("href")
            .Should().Be($"mailto:?subject={Text}&body={Text}%0D%0A{Url}");
    }

    private IRenderedComponent<SocialShare> CreateComponent()
    {
        return Render<SocialShare>(parameters => parameters
            .Add(component => component.UrlToShare, Url)
            .Add(component => component.TextToShare, Text));
    }
}
