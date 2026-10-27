using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System.Text.Json;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Shared.Mappers;
using Tff.Blog.Shared.Models;
using Tff.Blog.Shared.Models.Pages;
using Tff.Blog.Shared.Models.Posts;

namespace Tff.Blog.Api.Services;

public sealed class CmsService : ICmsService, IDisposable
{
    private readonly GraphQLHttpClient _graphQlClient;

    public CmsService(HttpClient httpClient, CmsSettings settings)
    {
        _graphQlClient = new GraphQLHttpClient(
            new GraphQLHttpClientOptions { EndPoint = new Uri(settings.GraphQlEndpoint) },
            new SystemTextJsonSerializer(),
            httpClient);
    }

    public async Task<List<PostModel>> GetPostsAsync(string publicationId, CancellationToken cancellationToken = default)
    {
        var query = $$"""
            query {
              publication(id: "{{publicationId}}") {
                id
                isHeadless
                posts(first: 20) {
                  pageInfo {
                    hasNextPage
                    endCursor
                  }
                  edges {
                    node {
                      id
                      slug
                      title
                      publishedAt
                      coverImage {
                        url
                      }
                      brief
                      url
                      content {
                        markdown
                      }
                      readTimeInMinutes
                      tags {
                        name
                      }
                    }
                  }
                }
              }
            }
            """;

        var response = await GetFromHashnodeAsync(query, cancellationToken);

        HashnodePostListApiResponse apiResponse = JsonSerializer.Deserialize<HashnodePostListApiResponse>(response.Data, JsonSettings.Options);

        var blogPosts = apiResponse.Publication.Posts?.Edges?.Select(edge => PostModelMapper.MapToBlogPostModel(edge.Node)).ToList();

        return blogPosts;
    }

    public async Task<List<PostModel>> GetSinglePostsAsync(string publicationId, string postSlug, CancellationToken cancellationToken = default)
    {
        var query = $$"""
            query {
              publication(id: "{{publicationId}}") {
                id
                isHeadless
                post(slug: "{{postSlug}}") {
                    id
                    slug
                    tags {
                      name
                    }
                    title
                    publishedAt
                    coverImage {
                      url
                    }
                    brief
                    url
                    content {
                      markdown
                    }
                    readTimeInMinutes
                }
              }
            }
            """;

        var response = await GetFromHashnodeAsync(query, cancellationToken);

        HashnodeSinglePostApiResponse apiResponse = JsonSerializer.Deserialize<HashnodeSinglePostApiResponse>(response.Data, JsonSettings.Options);

        var blogPost = PostModelMapper.MapToBlogPostModel(apiResponse.Publication.Post);

        if (blogPost == null)
        {
            return new List<PostModel>();
        }

        return new List<PostModel>
        {
            blogPost
        };
    }

    public async Task<PageModel> GetPageAsync(string publicationId, string pageSlug, CancellationToken cancellationToken = default)
    {
        var query = $$"""
            query {
              publication(id: "{{publicationId}}") {
                id
                isHeadless
                staticPage(slug: "{{pageSlug}}") {
                  id
                  slug
                  content {
                    markdown
                  }
                }
              }
            }
            """;

        var response = await GetFromHashnodeAsync(query, cancellationToken);

        HashnodePageApiResponse apiResponse = JsonSerializer.Deserialize<HashnodePageApiResponse>(response.Data, JsonSettings.Options);

        return PageModelMapper.MapToPageModel(apiResponse.Publication.StaticPage);
    }

    public void Dispose()
    {
        _graphQlClient.Dispose();
    }

    private async Task<GraphQLResponse<dynamic>> GetFromHashnodeAsync(string query, CancellationToken cancellationToken)
    {
        var graphQLRequest = new GraphQLRequest
        {
            Query = query
        };

        return await _graphQlClient.SendQueryAsync<dynamic>(graphQLRequest, cancellationToken);
    }
}
