namespace Tff.Blog.Api.Configuration;

public static class Settings
{
    public static bool GetShowDrafts()
    {
        return Convert.ToBoolean(GetEnvironmentVariable("ShowDrafts"));
    }

    private static string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }

    public static string GetHashnodePublicationId()
    {
        return GetEnvironmentVariable("HashnodePublicationId");
    }
}
