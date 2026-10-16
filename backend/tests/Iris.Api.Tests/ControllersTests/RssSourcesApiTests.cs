using System.Net;
using System.Net.Http.Json;
using Iris.Api.Contracts.RssSources;
using Iris.Api.Tests.Support;
using Iris.Application.Abstractions.Feeds;
using Iris.Domain.Entities;
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
    public async Task Create_WithDetails_PersistsAndReturnsDescriptionAndLogo()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Source test",
                feedUrl = "https://example.com/feed",
                countryCode = "FR",
                description = " Description test ",
                logoUrl = " https://example.com/logo.png "
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(created);

        using var getResponse = await client.GetAsync("/api/rss-sources");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var sources = await getResponse.Content
            .ReadFromJsonAsync<List<RssSourceResponse>>();

        Assert.NotNull(sources);

        var source = Assert.Single(sources);

        Assert.Equal(created.Id, source.Id);
        Assert.Equal("Description test", source.Description);
        Assert.Equal("https://example.com/logo.png", source.LogoUrl);
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
            null);

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
            null);

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
            null);

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
            null);

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
            isTimeout);

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
            null);

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

    [Fact]
    public async Task Collect_WhenUnexpectedErrorOccurs_ReturnsGenericInternalServerError()
    {
        await using var factory = new IrisApiFactory();

        const string internalMessage = "Erreur confidentielle de test";

        factory.FeedReadFailure = new InvalidOperationException(internalMessage);
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
            null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("Erreur interne du serveur", problem.Title);
        Assert.Equal("Une erreur inattendue est survenue.", problem.Detail);

        Assert.True(problem.Extensions.TryGetValue("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId?.ToString()));

        var content = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(internalMessage, content);
        Assert.DoesNotContain(nameof(InvalidOperationException), content);
    }

    [Fact]
    public async Task Update_WithValidData_PersistsChanges()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Source initiale",
                feedUrl = "https://example.com/feed",
                countryCode = "FR",
                description = "Ancienne description",
                logoUrl = "https://example.com/old-logo.png"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(created);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/rss-sources/{created.Id}",
            new
            {
                name = " Nouveau nom ",
                feedUrl = "https://example.com/feed",
                countryCode = "gb",
                isEnabled = false,
                description = (string?)null,
                logoUrl = (string?)null
            });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        using var getResponse = await client.GetAsync("/api/rss-sources");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var sources = await getResponse.Content
            .ReadFromJsonAsync<List<RssSourceResponse>>();

        Assert.NotNull(sources);

        var source = Assert.Single(sources);

        Assert.Equal(created.Id, source.Id);
        Assert.Equal("Nouveau nom", source.Name);
        Assert.Equal("https://example.com/feed", source.FeedUrl);
        Assert.Equal("GB", source.CountryCode);
        Assert.False(source.IsEnabled);
        Assert.Null(source.Description);
        Assert.Null(source.LogoUrl);
    }

    [Fact]
    public async Task Update_WithDuplicateFeedUrl_ReturnsConflictWithoutChangingSource()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var firstResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Première source",
                feedUrl = "https://example.com/first",
                countryCode = "FR"
            });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondResponse = await client.PostAsJsonAsync(
            "/api/rss-sources",
            new
            {
                name = "Deuxième source",
                feedUrl = "https://example.com/second",
                countryCode = "GB"
            });

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);

        var second = await secondResponse.Content
            .ReadFromJsonAsync<CreateRssSourceResponse>();

        Assert.NotNull(second);

        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/rss-sources/{second.Id}",
            new
            {
                name = "Nom modifié",
                feedUrl = "https://example.com/first",
                countryCode = "FR",
                isEnabled = false,
                description = "Modification refusée",
                logoUrl = "https://example.com/logo.png"
            });

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);

        using var getResponse = await client.GetAsync("/api/rss-sources");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var sources = await getResponse.Content
            .ReadFromJsonAsync<List<RssSourceResponse>>();

        Assert.NotNull(sources);
        Assert.Equal(2, sources.Count);

        var unchanged = Assert.Single(
            sources, source => source.Id == second.Id);

        Assert.Equal("Deuxième source", unchanged.Name);
        Assert.Equal("https://example.com/second", unchanged.FeedUrl);
        Assert.Equal("GB", unchanged.CountryCode);
        Assert.True(unchanged.IsEnabled);
        Assert.Null(unchanged.Description);
        Assert.Null(unchanged.LogoUrl);
    }

    [Fact]
    public async Task Update_WhenSourceIsUnknown_ReturnsNotFound()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var response = await client.PutAsJsonAsync(
            $"/api/rss-sources/{Guid.NewGuid()}",
            new
            {
                name = "Source inconnue",
                feedUrl = "https://example.com/feed",
                countryCode = "FR",
                isEnabled = true
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenSourceIsUnknown_ReturnsNotFound()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/rss-sources/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesSourceAndItsArticles_AndPreservesOtherSources()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        var sourceToDelete = new RssSource(
            "À supprimer",
            "https://example.com/delete",
            "FR");

        var sourceToKeep = new RssSource(
            "À conserver",
            "https://example.com/keep",
            "GB");

        var firstArticle = new Article(
            sourceToDelete.Id,
            "Premier article",
            "https://example.com/first");

        var secondArticle = new Article(
            sourceToDelete.Id,
            "Deuxième article",
            "https://example.com/second");

        var keptArticle = new Article(
            sourceToKeep.Id,
            "Article conservé",
            "https://example.com/kept");

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<IrisDbContext>();

            dbContext.RssSources.AddRange(sourceToDelete, sourceToKeep);
            await dbContext.SaveChangesAsync();

            dbContext.Articles.AddRange(
                firstArticle, secondArticle, keptArticle);

            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync(
            $"/api/rss-sources/{sourceToDelete.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        var remainingSources = await verificationContext.RssSources
            .AsNoTracking()
            .ToListAsync();

        var remainingSource = Assert.Single(remainingSources);
        Assert.Equal(sourceToKeep.Id, remainingSource.Id);

        var remainingArticles = await verificationContext.Articles
            .AsNoTracking()
            .ToListAsync();

        var remainingArticle = Assert.Single(remainingArticles);
        Assert.Equal(keptArticle.Id, remainingArticle.Id);
        Assert.Equal(sourceToKeep.Id, remainingArticle.RssSourceId);
    }
    
    [Fact]
    public async Task Collect_WhenSourceIsDisabled_ReturnsConflictWithoutReadingFeed()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        var source = new RssSource(
            "Source désactivée",
            "https://example.com/feed",
            "FR",
            isEnabled: false);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<IrisDbContext>();

            dbContext.RssSources.Add(source);
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        using var response = await client.PostAsync(
            $"/api/rss-sources/{source.Id}/collect",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content
            .ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Source RSS désactivée", problem.Title);
        Assert.Equal(0, factory.FeedReadCount);

        using var verificationScope = factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider
            .GetRequiredService<IrisDbContext>();

        Assert.Empty(await verificationContext.Articles.ToListAsync());
    }
}