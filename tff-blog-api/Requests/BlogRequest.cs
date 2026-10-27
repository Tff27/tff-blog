namespace Tff.Blog.Api.Requests;

internal sealed record BlogRequest(string PostName, string SortField, string SortOrder);
