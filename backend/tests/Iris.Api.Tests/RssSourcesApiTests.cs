using System.Net;
using System.Net.Http.Json;
using Iris.Api.Contracts.RssSources;

namespace Iris.Api.Tests;

public class RssSourcesApiTests
{
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

    public sealed class CreateRssSourceResponse
    {
        public Guid Id { get; init; }
    }
}