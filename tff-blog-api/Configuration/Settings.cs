using System;

namespace Tff.Blog.Api.Configuration;

public static class Settings
{
    public static string GetRepoName() 
    {
        return GetEnvironmentVariable("RepoName");
    }

    public static string GetRepoPostsPath()
    {
        return GetEnvironmentVariable("RepoPostsPath");
    }

    public static string GetGitToken()
    {
        return GetEnvironmentVariable("GitToken");
    }

    public static bool GetShowDrafts()
    {
        return Convert.ToBoolean(GetEnvironmentVariable("ShowDrafts"));
    }

    private static string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }

    public static bool GetUseHashnodeCmsApi()
    {
        return Convert.ToBoolean(GetEnvironmentVariable("UseHashnodeCmsApi"));
    }

    public static string GetHashnodePublicationId()
    {
        return GetEnvironmentVariable("HashnodePublicationId");
    }
}
