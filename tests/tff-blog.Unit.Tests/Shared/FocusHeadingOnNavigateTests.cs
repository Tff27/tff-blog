using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Tff.Blog.Client.Shared;

namespace Tff.Blog.Client.Unit.Tests.Shared;

public class FocusHeadingOnNavigateTests : BunitContext
{
    private const string FocusSelectorIdentifier = "Blazor._internal.domWrapper.focusBySelector";

    public FocusHeadingOnNavigateTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void FocusHeadingOnNavigate_OnTheInitialRender_DoesNotFocusTheHeading()
    {
        Render<FocusHeadingOnNavigate>(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<FirstPage>()));

        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == FocusSelectorIdentifier);
    }

    [Fact]
    public void FocusHeadingOnNavigate_OnTheInitialRender_IsNotRenderedIntoThePage()
    {
        var cut = Render<FocusHeadingOnNavigate>(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<FirstPage>()));

        cut.Markup.Should().BeEmpty();
    }

    [Fact]
    public void FocusHeadingOnNavigate_AfterNavigatingToAnotherPage_FocusesTheSelector()
    {
        var cut = Render<FocusHeadingOnNavigate>(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<FirstPage>()));

        cut.Render(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<SecondPage>()));

        var invocation = JSInterop.VerifyInvoke(FocusSelectorIdentifier);

        invocation.Arguments.Should().ContainSingle().Which.Should().Be("h1");
    }

    [Fact]
    public void FocusHeadingOnNavigate_WhenTheSamePageRerenders_DoesNotFocus()
    {
        var cut = Render<FocusHeadingOnNavigate>(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<FirstPage>()));

        cut.Render(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<FirstPage>()));

        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == FocusSelectorIdentifier);
    }

    [Fact]
    public void FocusHeadingOnNavigate_UsesTheConfiguredSelector()
    {
        var cut = Render<FocusHeadingOnNavigate>(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<FirstPage>())
            .Add(component => component.Selector, "main h1"));

        cut.Render(parameters => parameters
            .Add(component => component.RouteData, RouteDataFor<SecondPage>()));

        var invocation = JSInterop.VerifyInvoke(FocusSelectorIdentifier);

        invocation.Arguments.Should().ContainSingle().Which.Should().Be("main h1");
    }

    private static RouteData RouteDataFor<T>() where T : IComponent => new(typeof(T), new Dictionary<string, object?>());

    private sealed class FirstPage : ComponentBase
    {
    }

    private sealed class SecondPage : ComponentBase
    {
    }
}
