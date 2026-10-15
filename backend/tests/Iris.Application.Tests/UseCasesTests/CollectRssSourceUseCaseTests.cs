using Iris.Application.Abstractions.Feeds;
using Iris.Application.Tests.Support.Fakes;
using Iris.Application.UseCases.RssSources;
using Iris.Domain.Entities;

namespace Iris.Application.Tests.UseCasesTests;

public class CollectRssSourceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSourceIsUnknown_ReturnsNullWithoutReadingFeed()
    {
        var sources = new FakeRssSourceRepository();
        var reader = new FakeRssFeedReader();
        var articles = new FakeArticleRepository();
        var useCase = new CollectRssSourceUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Null(result);
        Assert.Equal(0, reader.CallCount);
        Assert.Empty(articles.Articles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFeedIsEmpty_ReturnsZero()
    {
        var source = CreateSource();
        var sources = new FakeRssSourceRepository(source);
        var reader = new FakeRssFeedReader();
        var articles = new FakeArticleRepository();
        var useCase = new CollectRssSourceUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync(source.Id);

        Assert.Equal((int?)0, result);
        Assert.Equal(1, reader.CallCount);
        Assert.Equal(source.FeedUrl, reader.LastFeedUrl);
        Assert.Empty(articles.Articles);
    }

    [Fact]
    public async Task ExecuteAsync_WithNewItems_PersistsArticlesAndReturnsCount()
    {
        var source = CreateSource();
        var publishedAt = new DateTimeOffset(
            2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(2));

        var firstItem = new RssFeedItem
        {
            Title = "Premier article",
            ArticleUrl = "https://example.com/first",
            PublishedAt = publishedAt
        };

        var secondItem = new RssFeedItem
        {
            Title = "Deuxième article",
            ArticleUrl = "https://example.com/second"
        };

        var sources = new FakeRssSourceRepository(source);
        var reader = new FakeRssFeedReader(firstItem, secondItem);
        var articles = new FakeArticleRepository();
        var useCase = new CollectRssSourceUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync(source.Id);

        Assert.Equal((int?)2, result);
        Assert.Equal(1, reader.CallCount);
        Assert.Equal(source.FeedUrl, reader.LastFeedUrl);

        Assert.Collection(
            articles.Articles,
            first =>
            {
                Assert.NotEqual(Guid.Empty, first.Id);
                Assert.Equal(source.Id, first.RssSourceId);
                Assert.Equal(firstItem.Title, first.Title);
                Assert.Equal(firstItem.ArticleUrl, first.ArticleUrl);
                Assert.NotNull(first.PublishedAt);
                Assert.Equal(
                    publishedAt.ToUniversalTime(),
                    first.PublishedAt.Value);
                Assert.Equal(TimeSpan.Zero, first.PublishedAt.Value.Offset);
            },
            second =>
            {
                Assert.NotEqual(Guid.Empty, second.Id);
                Assert.Equal(source.Id, second.RssSourceId);
                Assert.Equal(secondItem.Title, second.Title);
                Assert.Equal(secondItem.ArticleUrl, second.ArticleUrl);
                Assert.Null(second.PublishedAt);
            });
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingArticle_AddsOnlyNewArticle()
    {
        var source = CreateSource();
        var existing = new Article(
            source.Id,
            "Article existant",
            "https://example.com/existing");

        var sources = new FakeRssSourceRepository(source);
        var reader = new FakeRssFeedReader(
            new RssFeedItem
            {
                Title = "Article existant",
                ArticleUrl = existing.ArticleUrl
            },
            new RssFeedItem
            {
                Title = "Nouvel article",
                ArticleUrl = "https://example.com/new"
            });

        var articles = new FakeArticleRepository(existing);
        var useCase = new CollectRssSourceUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync(source.Id);

        Assert.Equal((int?)1, result);
        Assert.Equal(2, articles.Articles.Count);

        var retained = Assert.Single(
            articles.Articles,
            article => article.ArticleUrl == existing.ArticleUrl);

        Assert.Same(existing, retained);

        var added = Assert.Single(
            articles.Articles,
            article => article.ArticleUrl == "https://example.com/new");

        Assert.Equal(source.Id, added.RssSourceId);
        Assert.Equal("Nouvel article", added.Title);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateFeedItems_AddsArticleOnlyOnce()
    {
        var source = CreateSource();
        var sources = new FakeRssSourceRepository(source);
        var reader = new FakeRssFeedReader(
            new RssFeedItem
            {
                Title = "Premier titre",
                ArticleUrl = "https://example.com/article"
            },
            new RssFeedItem
            {
                Title = "Autre titre",
                ArticleUrl = "https://example.com/article"
            });

        var articles = new FakeArticleRepository();
        var useCase = new CollectRssSourceUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync(source.Id);

        Assert.Equal((int?)1, result);

        var saved = Assert.Single(articles.Articles);
        Assert.Equal("Premier titre", saved.Title);
        Assert.Equal("https://example.com/article", saved.ArticleUrl);
        Assert.Equal(source.Id, saved.RssSourceId);
    }

    private static RssSource CreateSource()
    {
        return new RssSource(
            "Source test",
            "https://example.com/feed",
            "FR");
    }
}