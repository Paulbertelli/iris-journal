using System.Net;
using System.Net.Http.Json;

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

    public sealed class CreateRssSourceResponse
    {
        public Guid Id { get; init; }
    }
}