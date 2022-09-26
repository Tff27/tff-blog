using System.Text.RegularExpressions;
using Tff.Blog.Shared.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tff.Blog.Shared.Converters
{
    public static class MarkdownToModelConverter
    {
        public static T CreateModelFromMarkdown<T>(string markdown) where T : MarkdownModel, new()
        {
            var YamlDeserializer = new DeserializerBuilder()
              .WithNamingConvention(CamelCaseNamingConvention.Instance)
              .IgnoreUnmatchedProperties()
              .Build();

            var expression = "(?:---\\r?\\n)(?<frontmatter>[\\s\\S]*?)(?:---\\r?\\n)";
            var result = Regex.Match(markdown, expression).Groups.GetValueOrDefault("frontmatter")?.Value;

            var model = new T();

            if (!string.IsNullOrEmpty(result))
            {
                model = YamlDeserializer.Deserialize<T>(result);
            }

            Regex regex = new(expression);
            model.Text = regex.Replace(markdown, string.Empty).Trim();

            return model;
        }
    }
}
