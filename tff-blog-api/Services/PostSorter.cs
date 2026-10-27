using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Services;

internal static class PostSorter
{
    private static readonly Dictionary<string, Func<PostModel, object>> SortSelectors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Date"] = post => post.Date,
        ["Title"] = post => post.Title,
        ["Slug"] = post => post.Slug,
        ["Description"] = post => post.Description,
        ["Image"] = post => post.Image,
        ["Text"] = post => post.Text,
        ["ReadTimeInMinutes"] = post => post.ReadTimeInMinutes,
        ["Draft"] = post => post.Draft,
    };

    internal static bool IsValidSortOrder(string sortOrder)
    {
        return string.IsNullOrEmpty(sortOrder)
            || string.Equals(sortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase)
            || string.Equals(sortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase);
    }

    internal static bool IsValidSortField(string sortField)
    {
        return !string.IsNullOrEmpty(sortField) && SortSelectors.ContainsKey(sortField);
    }

    internal static List<PostModel> Sort(List<PostModel> postList, string sortField, string sortOrder)
    {
        if (!SortSelectors.TryGetValue(sortField ?? string.Empty, out var selector))
        {
            throw new ArgumentException(
                $"The sort field \"{sortField}\" is invalid. Supported fields: {string.Join(", ", SortSelectors.Keys)}.",
                nameof(sortField));
        }

        if (string.Equals(sortOrder, "Ascending", StringComparison.InvariantCultureIgnoreCase))
        {
            return postList.OrderBy(selector).ToList();
        }

        if (string.Equals(sortOrder, "Descending", StringComparison.InvariantCultureIgnoreCase))
        {
            return postList.OrderByDescending(selector).ToList();
        }

        return postList;
    }
}
