using System;
using System.Text.Json;

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
        var envVar = Environment.GetEnvironmentVariable(name);

        if (envVar == null)
        {
            var x = JsonSerializer.Serialize(Environment.GetEnvironmentVariables());

            throw new Exception($"Variable not found {name} available: {x}");
        }

        return envVar;
    }
}
