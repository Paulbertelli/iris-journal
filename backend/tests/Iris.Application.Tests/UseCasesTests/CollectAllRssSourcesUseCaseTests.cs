using Iris.Application.Abstractions.Feeds;
using Iris.Application.Tests.Support.Fakes;
using Iris.Application.UseCases.RssSources;
using Iris.Domain.Entities;

namespace Iris.Application.Tests.UseCasesTests;

public class CollectAllRssSourcesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithoutSources_ReturnsEmptyResult()
    {
        var sources = new FakeRssSourceRepository();
        var reader = new FakeRssFeedReader();
        var articles = new FakeArticleRepository();
        var useCase = CreateUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync();

        Assert.Empty(result.Sources);
        Assert.Equal(0, result.TotalAddedCount);
        Assert.Equal(0, reader.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_CollectsActiveSourcesAndSkipsDisabledSources()
    {
        var first = CreateSource("Alpha");
        var second = CreateSource("Beta");
        var disabled = CreateSource("Gamma", isEnabled: false);

        var sources = new FakeRssSourceRepository(
            first, second, disabled);

        var reader = new FakeRssFeedReader
        {
            ReadHandler = (url, _) =>
                Task.FromResult<IReadOnlyList<RssFeedItem>>(
                [
                    new RssFeedItem
                    {
                        Title = "Article test",
                        ArticleUrl = url + "/article"
                    }
                ])
        };

        var articles = new FakeArticleRepository();
        var useCase = CreateUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, reader.CallCount);
        Assert.Equal(2, result.TotalAddedCount);
        Assert.Equal(
            [first.Id, second.Id],
            result.Sources.Select(source => source.SourceId));

        Assert.All(result.Sources, source =>
        {
            Assert.Equal(SourceCollectionStatus.Collected, source.Status);
            Assert.Equal(1, source.AddedCount);
            Assert.Null(source.Error);
        });

        Assert.Equal(
            [first.Id, second.Id],
            articles.Articles.Select(article => article.RssSourceId));
    }

    [Theory]
    [InlineData(false, SourceCollectionStatus.Failed)]
    [InlineData(true, SourceCollectionStatus.Timeout)]
    public async Task ExecuteAsync_WhenFeedFails_ContinuesWithNextSource(
        bool isTimeout,
        SourceCollectionStatus expectedStatus)
    {
        var failing = CreateSource("Alpha");
        var successful = CreateSource("Beta");

        var sources = new FakeRssSourceRepository(failing, successful);
        var failure = new FeedReadException(
            "Flux indisponible.",
            new HttpRequestException("Erreur simulée."),
            isTimeout);

        var reader = new FakeRssFeedReader
        {
            ReadHandler = (url, _) =>
                url == failing.FeedUrl
                    ? Task.FromException<IReadOnlyList<RssFeedItem>>(failure)
                    : Task.FromResult<IReadOnlyList<RssFeedItem>>(
                    [
                        new RssFeedItem
                        {
                            Title = "Article disponible",
                            ArticleUrl = "https://example.com/article"
                        }
                    ])
        };

        var articles = new FakeArticleRepository();
        var useCase = CreateUseCase(sources, reader, articles);

        var result = await useCase.ExecuteAsync();

        Assert.Equal(2, reader.CallCount);
        Assert.Equal(1, result.TotalAddedCount);

        Assert.Collection(
            result.Sources,
            source =>
            {
                Assert.Equal(failing.Id, source.SourceId);
                Assert.Equal(expectedStatus, source.Status);
                Assert.Equal(0, source.AddedCount);
                Assert.Equal(failure.Message, source.Error);
            },
            source =>
            {
                Assert.Equal(successful.Id, source.SourceId);
                Assert.Equal(SourceCollectionStatus.Collected, source.Status);
                Assert.Equal(1, source.AddedCount);
            });

        Assert.Equal(
            successful.Id,
            Assert.Single(articles.Articles).RssSourceId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledDuringFeedRead_StopsCollection()
    {
        using var cancellation = new CancellationTokenSource();

        var sources = new FakeRssSourceRepository(
            CreateSource("Alpha"),
            CreateSource("Beta"));

        var reader = new FakeRssFeedReader
        {
            ReadHandler = (_, token) =>
            {
                cancellation.Cancel();

                return Task.FromCanceled<IReadOnlyList<RssFeedItem>>(token);
            }
        };

        var articles = new FakeArticleRepository();
        var useCase = CreateUseCase(sources, reader, articles);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => useCase.ExecuteAsync(cancellation.Token));

        Assert.Equal(1, reader.CallCount);
        Assert.Empty(articles.Articles);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUnexpectedErrorOccurs_PropagatesError()
    {
        var sources = new FakeRssSourceRepository(
            CreateSource("Alpha"),
            CreateSource("Beta"));

        var failure = new InvalidOperationException("Erreur inattendue.");

        var reader = new FakeRssFeedReader
        {
            ReadHandler = (_, _) =>
                Task.FromException<IReadOnlyList<RssFeedItem>>(failure)
        };

        var articles = new FakeArticleRepository();
        var useCase = CreateUseCase(sources, reader, articles);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync());

        Assert.Same(failure, exception);
        Assert.Equal(1, reader.CallCount);
        Assert.Empty(articles.Articles);
    }

    private static CollectAllRssSourcesUseCase CreateUseCase(
        FakeRssSourceRepository sources,
        FakeRssFeedReader reader,
        FakeArticleRepository articles)
    {
        var collectOne = new CollectRssSourceUseCase(
            sources, reader, articles);

        return new CollectAllRssSourcesUseCase(sources, collectOne);
    }

    private static RssSource CreateSource(
        string name,
        bool isEnabled = true)
    {
        return new RssSource(
            name,
            $"https://example.com/{name.ToLowerInvariant()}",
            "FR",
            isEnabled: isEnabled);
    }
}