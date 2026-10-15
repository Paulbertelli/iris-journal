using System.Net;
using System.Net.Http.Json;
using Iris.Api.Contracts.RssSources;
using Iris.Api.Tests.Support;
using Iris.Application.Abstractions.Feeds;
using Iris.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Api.Tests.ControllersTests;

public class RssSourcesApiTests
{
    public sealed class CreateRssSourceResponse
    {
        public Guid Id { get; init; }
    }

    public sealed class CollectRssSourceResponse
    {
        public int AddedCount { get; init; }
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedWithId()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Le Monde",
                feedUrl = "https://example.com/feed",
                countryCode = "FR"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task Create_WithDuplicateFeedUrl_ReturnsConflict()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        var request = new
        {
            name = "Le Monde",
            feedUrl = "https://example.com/feed",
            countryCode = "FR"
        };

        using var firstResponse = await client.PostAsJsonAsync(
            "/api/rss-sources", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondResponse = await client.PostAsJsonAsync(
            "/api/rss-sources", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidFeedUrl_ReturnsBadRequest()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Source test",
                feedUrl = "adresse-invalide",
                countryCode = "FR"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WhenDatabaseIsEmpty_ReturnsOkAndEmptyList()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/rss-sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<RssSourceResponse>>();

        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetAll_WhenSourcesExist_ReturnsSourcesOrderedByName()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        var sourcesToCreate = new[]
        {
            new { name = "Zeta", feedUrl = "https://zeta.com/feed1", countryCode = "FR" },
            new { name = "Alpha", feedUrl = "https://alpha.com/feed2", countryCode = "GB" }
        };

        var createdIds = new Dictionary<string, Guid>();

        foreach (var source in sourcesToCreate)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/rss-sources", source);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content
                .ReadFromJsonAsync<CreateRssSourceResponse>();

            Assert.NotNull(created);
            createdIds.Add(source.name, created.Id);
        }

        using var getResponse = await client.GetAsync("/api/rss-sources");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content
            .ReadFromJsonAsync<List<RssSourceResponse>>();

        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Equal(
            ["Alpha", "Zeta"],
            body.Select(source => source.Name));

        foreach (var expected in sourcesToCreate)
        {
            var actual = Assert.Single(body, source => source.Name == expected.name);

            Assert.Equal(createdIds[expected.name], actual.Id);
            Assert.Equal(expected.feedUrl, actual.FeedUrl);
            Assert.Equal(expected.countryCode, actual.CountryCode);
            Assert.True(actual.IsEnabled);
        }
    }

    [Fact]
    public async Task Collect_WhenSourceIsUnknown_ReturnsNotFoundWithoutReadingFeed()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/rss-sources/{Guid.NewGuid()}/collect",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, factory.FeedReadCount);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        Assert.Empty(await dbContext.Articles.ToListAsync());
    }

    [Fact]
    public async Task Collect_WithNewItems_PersistsArticlesAndReturnsAddedCount()
    {
        await using var factory = new IrisApiFactory();

        factory.FeedItems =
        [
            new RssFeedItem
            {
                Title = "Premier article",
                ArticleUrl = "https://example.com/first",
                PublishedAt = new DateTimeOffset(
                    2026, 10, 15, 10, 0, 0, TimeSpan.Zero)
            },
            new RssFeedItem
            {
                Title = "Deuxième article",
                ArticleUrl = "https://example.com/second"
            }
        ];

        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Source test",
                feedUrl = "https://example.com/feed",
                countryCode = "FR"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var source = await createResponse.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(source);

        using var response = await client.PostAsync(
            $"/api/rss-sources/{source.Id}/collect",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<CollectRssSourceResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.AddedCount);
        Assert.Equal(1, factory.FeedReadCount);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        var articles = await dbContext.Articles
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(2, articles.Count);

        foreach (var expected in factory.FeedItems)
        {
            var article = Assert.Single(
                articles,
                article => article.ArticleUrl == expected.ArticleUrl);

            Assert.NotEqual(Guid.Empty, article.Id);
            Assert.Equal(source.Id, article.RssSourceId);
            Assert.Equal(expected.Title, article.Title);
            Assert.Equal(expected.PublishedAt, article.PublishedAt);
        }
    }

    [Fact]
    public async Task Collect_WhenRepeated_ReturnsZeroAndPreservesExistingArticles()
    {
        await using var factory = new IrisApiFactory();

        factory.FeedItems =
        [
            new RssFeedItem
            {
                Title = "Premier article",
                ArticleUrl = "https://example.com/first"
            },
            new RssFeedItem
            {
                Title = "Deuxième article",
                ArticleUrl = "https://example.com/second"
            }
        ];

        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Source test",
                feedUrl = "https://example.com/feed",
                countryCode = "FR"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var source = await createResponse.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(source);

        using var firstResponse = await client.PostAsync(
            $"/api/rss-sources/{source.Id}/collect",
            content: null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstResult = await firstResponse.Content
            .ReadFromJsonAsync<CollectRssSourceResponse>();

        Assert.NotNull(firstResult);
        Assert.Equal(2, firstResult.AddedCount);

        List<Guid> initialIds;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<IrisDbContext>();

            initialIds = await dbContext.Articles
                .Select(article => article.Id)
                .ToListAsync();
        }

        Assert.Equal(2, initialIds.Count);

        using var secondResponse = await client.PostAsync(
            $"/api/rss-sources/{source.Id}/collect",
            content: null);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondResult = await secondResponse.Content
            .ReadFromJsonAsync<CollectRssSourceResponse>();

        Assert.NotNull(secondResult);
        Assert.Equal(0, secondResult.AddedCount);
        Assert.Equal(2, factory.FeedReadCount);

        using var finalScope = factory.Services.CreateScope();
        var finalDbContext = finalScope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        var savedArticles = await finalDbContext.Articles
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(
            initialIds.OrderBy(id => id),
            savedArticles.Select(article => article.Id).OrderBy(id => id));

        Assert.All(
            savedArticles,
            article => Assert.Equal(source.Id, article.RssSourceId));
    }

    [Theory]
    [InlineData(false, HttpStatusCode.BadGateway)]
    [InlineData(true, HttpStatusCode.GatewayTimeout)]
    public async Task Collect_WhenFeedReadFails_ReturnsExpectedError(
        bool isTimeout,
        HttpStatusCode expectedStatus)
    {
        await using var factory = new IrisApiFactory();

        factory.FeedReadFailure = new FeedReadException(
            "Échec de lecture du flux.",
            new Exception("Détail interne confidentiel"),
            isTimeout: isTimeout);

        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Source test",
                feedUrl = "https://example.com/feed",
                countryCode = "FR"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var source = await createResponse.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(source);

        using var response = await client.PostAsync(
            $"/api/rss-sources/{source.Id}/collect",
            content: null);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal((int?)expectedStatus, problem.Status);
        Assert.Equal("Échec de la collecte RSS", problem.Title);
        Assert.Equal("Échec de lecture du flux.", problem.Detail);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Détail interne confidentiel", content);

        Assert.Equal(1, factory.FeedReadCount);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        Assert.Empty(await dbContext.Articles.ToListAsync());
    }
}