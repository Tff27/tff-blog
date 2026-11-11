using AwesomeAssertions;
using Tff.Blog.Client.ViewModels;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Unit.Tests.ViewModels;

public class PostViewModelTests
{
    [Fact]
    public void FormattedDate_UsesAnInvariantLongDate()
    {
        var post = CreatePost(new DateTime(2024, 5, 6, 11, 3, 6));

        post.FormattedDate.Should().Be("May 6, 2024");
    }

    [Fact]
    public void IsoDate_KeepsTheRoundTripFormat()
    {
        var date = new DateTime(2024, 5, 6, 11, 3, 6, DateTimeKind.Utc);

        CreatePost(date).IsoDate.Should().Be(date.ToString("O"));
    }

    [Fact]
    public void ReadTime_UsesTheProvidedMinutes()
    {
        CreatePost(readTimeInMinutes: 6).ReadTime.Should().Be("6 min read");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void ReadTime_ClampsInvalidValuesToOneMinute(int minutes)
    {
        CreatePost(readTimeInMinutes: minutes).ReadTime.Should().Be("1 min read");
    }

    [Fact]
    public void Url_IsTheRelativePostRoute()
    {
        CreatePost(slug: "test-post").Url.Should().Be("post/test-post");
    }

    [Fact]
    public void Properties_AreForwardedFromTheModel()
    {
        var post = CreatePost();

        post.Title.Should().Be("Test post");
        post.Description.Should().Be("A short description");
        post.Image.Should().Be("https://example.com/cover.png");
        post.Text.Should().Be("Body");
        post.Tags.Should().ContainInOrder("dotnet", "blazor");
    }

    [Fact]
    public void ShareDescription_UsesTheSharedTruncation()
    {
        CreatePost(description: "A short description").ShareDescription.Should().Be("A short description");
    }

    [Fact]
    public void ShareDescription_TruncatesLongDescriptionsWithAnEllipsis()
    {
        var description = new string('a', 190) + " supercalifragilistic tail";

        CreatePost(description: description).ShareDescription.Should().Be(new string('a', 190) + "…");
    }

    private static PostViewModel CreatePost(
        DateTime? date = null,
        string slug = "test-post",
        int readTimeInMinutes = 1,
        string? description = "A short description")
    {
        return new PostViewModel(new PostModel
        {
            Title = "Test post",
            Slug = slug,
            Description = description!,
            Image = "https://example.com/cover.png",
            Text = "Body",
            Date = date ?? new DateTime(2024, 1, 1),
            ReadTimeInMinutes = readTimeInMinutes,
            Tags = ["dotnet", "blazor"],
        });
    }
}
