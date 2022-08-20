using System;

namespace Tff.Blog.Api.Configuration
{
    public static class Settings
    {
        private static readonly string appName = "tff-blog";
        private static readonly string repoOwner = "tff27";
        private static readonly string repoPostsPath = "wwwroot/posts";

        public static string GetRepoName() 
        {
            return GetEnvironmentVariable("RepoName");
        }

        public static string GetRepoPostsPath()
        {
            return GetEnvironmentVariable("RepoPostsPath");
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
