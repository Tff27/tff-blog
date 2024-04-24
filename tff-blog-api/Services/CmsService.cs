using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Tff.Blog.Api.Configuration;
using Tff.Blog.Shared.Mappers;
using Tff.Blog.Shared.Models;
using Tff.Blog.Shared.Models.Pages;
using Tff.Blog.Shared.Models.Posts;

namespace Tff.Blog.Api.Services;

internal class CmsService
{
    private static readonly string graphQLEndpoint = "https://gql.hashnode.com";

    public static async Task<List<PostModel>> GetPostsAsync(string publicationId)
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

        var response = await GetFromHashnodeAsync(query);

        HashnodePostListApiResponse apiResponse = JsonSerializer.Deserialize<HashnodePostListApiResponse>(response.Data, JsonSettings.Options);

        var blogPosts = apiResponse.Publication.Posts?.Edges?.Select(edge => PostModelMapper.MapToBlogPostModel(edge.Node)).ToList();

        return blogPosts;
    }

    public static async Task<List<PostModel>> GetSinglePostsAsync(string publicationId, string postSlug)
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

        var response = await GetFromHashnodeAsync(query);

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

    public static async Task<PageModel> GetPageAsync(string publicationId, string pageSlug)
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

        var response = await GetFromHashnodeAsync(query);

        HashnodePageApiResponse apiResponse = JsonSerializer.Deserialize<HashnodePageApiResponse>(response.Data, JsonSettings.Options);

        return PageModelMapper.MapToPageModel(apiResponse.Publication.StaticPage);
    }

    private static async Task<GraphQLResponse<dynamic>> GetFromHashnodeAsync(string query)
    {
        var httpClient = new HttpClient();
        var graphQLClient = new GraphQLHttpClient(new GraphQLHttpClientOptions { EndPoint = new Uri(graphQLEndpoint) }, new SystemTextJsonSerializer(), httpClient);

        var graphQLRequest = new GraphQLRequest
        {
            Query = query
        };

        return await graphQLClient.SendQueryAsync<dynamic>(graphQLRequest);
    }
}