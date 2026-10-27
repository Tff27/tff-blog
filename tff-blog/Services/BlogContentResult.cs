namespace Tff.Blog.Client.Services;

public sealed record BlogContentResult<T>(T? Content, bool IsNotFound = false, string? ErrorMessage = null)
    where T : class
{
    public bool IsSuccess => ErrorMessage is null && !IsNotFound;

    public static BlogContentResult<T> Success(T content) => new(content);

    public static BlogContentResult<T> NotFound() => new(default, IsNotFound: true);

    public static BlogContentResult<T> Failure(string errorMessage) => new(default, ErrorMessage: errorMessage);
}
