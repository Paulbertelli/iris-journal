using System.Net;
using System.Net.Http.Json;
using Iris.Api.Contracts.Articles;
using Iris.Api.Tests.Support;
using Iris.Domain.Entities;
using Iris.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Iris.Api.Tests.ControllersTests;

public class ArticlesApiTests
{
    [Fact]
    public async Task GetLatest_WhenDatabaseIsEmpty_ReturnsOkAndEmptyList()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<ArticleResponse>>();

        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    [InlineData("abc")]
    public async Task GetLatest_WithInvalidLimit_ReturnsBadRequest(string limit)
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(
            $"/api/articles?limit={limit}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLatest_ReturnsArticlesOrderedAndLimited()
    {
        await using var factory = new IrisApiFactory();
        factory.CreateDatabase();

        var source = new RssSource(
            "Source test", "https://example.com/feed", "FR");

        var publishedAt = new DateTimeOffset(
            2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

        var oldest = new Article(
            source.Id, "Ancien", "https://example.com/oldest");
        var middle = new Article(
            source.Id, "Intermédiaire", "https://example.com/middle");
        var newest = new Article(
            source.Id, "Récent", "https://example.com/newest", publishedAt);

        var collectedAt = new DateTimeOffset(
            2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<IrisDbContext>();

            dbContext.RssSources.Add(source);
            await dbContext.SaveChangesAsync();

            dbContext.Articles.AddRange(middle, oldest, newest);

            dbContext.Entry(oldest)
                .Property(article => article.CollectedAt)
                .CurrentValue = collectedAt;

            dbContext.Entry(middle)
                .Property(article => article.CollectedAt)
                .CurrentValue = collectedAt.AddMinutes(1);

            dbContext.Entry(newest)
                .Property(article => article.CollectedAt)
                .CurrentValue = collectedAt.AddMinutes(2);

            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/articles?limit=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<List<ArticleResponse>>();

        Assert.NotNull(body);
        Assert.Equal(
            [newest.Id, middle.Id],
            body.Select(article => article.Id));

        var first = body[0];
        Assert.Equal(source.Id, first.RssSourceId);
        Assert.Equal("Récent", first.Title);
        Assert.Equal("https://example.com/newest", first.ArticleUrl);
        Assert.Equal(publishedAt, first.PublishedAt);
        Assert.Equal(collectedAt.AddMinutes(2), first.CollectedAt);

        Assert.Null(body[1].PublishedAt);
    }
}