using AwesomeAssertions;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Sorting;

public class PostSorterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ascending")]
    [InlineData("ascending")]
    [InlineData("Descending")]
    [InlineData("DESCENDING")]
    public void IsValidSortOrder_ReturnsTrue_ForSupportedValues(string? sortOrder)
    {
        PostSorter.IsValidSortOrder(sortOrder!).Should().BeTrue();
    }

    [Theory]
    [InlineData("Sideways")]
    [InlineData("asc")]
    [InlineData(" ")]
    public void IsValidSortOrder_ReturnsFalse_ForUnsupportedValues(string sortOrder)
    {
        PostSorter.IsValidSortOrder(sortOrder).Should().BeFalse();
    }

    [Theory]
    [InlineData("Date")]
    [InlineData("title")]
    [InlineData("SLUG")]
    [InlineData("Description")]
    [InlineData("ReadTimeInMinutes")]
    [InlineData("Draft")]
    public void IsValidSortField_ReturnsTrue_ForWhitelistedFields(string sortField)
    {
        PostSorter.IsValidSortField(sortField).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Tags")]
    [InlineData("Image")]
    [InlineData("Text")]
    [InlineData("Unknown")]
    public void IsValidSortField_ReturnsFalse_ForUnknownFields(string? sortField)
    {
        PostSorter.IsValidSortField(sortField!).Should().BeFalse();
    }

    [Fact]
    public void Sort_ByDateAscending_OrdersOldestFirst()
    {
        var posts = CreatePosts();

        var sorted = PostSorter.Sort(posts, "Date", "Ascending");

        sorted.Select(post => post.Title).Should().ContainInOrder("Oldest", "Middle", "Newest");
    }

    [Fact]
    public void Sort_ByDateDescending_OrdersNewestFirst()
    {
        var posts = CreatePosts();

        var sorted = PostSorter.Sort(posts, "Date", "Descending");

        sorted.Select(post => post.Title).Should().ContainInOrder("Newest", "Middle", "Oldest");
    }

    [Fact]
    public void Sort_ByTitleAscending_IsCaseInsensitiveOnFieldName()
    {
        var posts = CreatePosts();

        var sorted = PostSorter.Sort(posts, "title", "ascending");

        sorted.Select(post => post.Title).Should().ContainInOrder("Middle", "Newest", "Oldest");
    }

    [Fact]
    public void Sort_WithUnknownOrder_ReturnsInputUnchanged()
    {
        var posts = CreatePosts();

        var sorted = PostSorter.Sort(posts, "Title", "Sideways");

        sorted.Should().BeSameAs(posts);
    }

    [Fact]
    public void Sort_WithUnknownField_ThrowsArgumentException()
    {
        var posts = CreatePosts();

        var act = () => PostSorter.Sort(posts, "Tags", "Ascending");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("sortField")
            .WithMessage("*Tags*");
    }

    [Fact]
    public void Sort_WithNullField_ThrowsArgumentException()
    {
        var posts = CreatePosts();

        var act = () => PostSorter.Sort(posts, null!, "Ascending");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("sortField");
    }

    [Fact]
    public void Sort_DoesNotMutateTheInputList()
    {
        var posts = CreatePosts();
        var originalOrder = posts.Select(post => post.Title).ToList();

        PostSorter.Sort(posts, "Title", "Ascending");

        posts.Select(post => post.Title).Should().ContainInOrder(originalOrder);
    }

    private static List<PostModel> CreatePosts()
    {
        return new List<PostModel>
        {
            new()
            {
                Title = "Newest",
                Slug = "newest",
                Date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                ReadTimeInMinutes = 1,
            },
            new()
            {
                Title = "Oldest",
                Slug = "oldest",
                Date = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ReadTimeInMinutes = 9,
            },
            new()
            {
                Title = "Middle",
                Slug = "middle",
                Date = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                ReadTimeInMinutes = 5,
            },
        };
    }
}
