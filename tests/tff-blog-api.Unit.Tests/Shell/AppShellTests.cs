using AwesomeAssertions;
using Tff.Blog.Api.Shell;

namespace Tff.Blog.Api.Unit.Tests.Shell;

public class AppShellTests
{
    [Fact]
    public void Html_IsTheEmbeddedClientIndexPage()
    {
        var html = AppShell.Html;

        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<head>");
        html.Should().Contain("</head>");
        html.Should().Contain("<div id=\"app\">");
        html.Should().Contain("_framework/blazor.webassembly.js");
    }

    [Fact]
    public void Html_IsStableAcrossCalls()
    {
        AppShell.Html.Should().BeSameAs(AppShell.Html);
    }
}
