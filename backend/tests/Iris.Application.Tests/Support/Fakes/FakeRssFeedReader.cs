using Iris.Application.Abstractions.Feeds;

namespace Iris.Application.Tests.Support.Fakes;

public sealed class FakeRssFeedReader(
    params RssFeedItem[] items) : IRssFeedReader
{
    public int CallCount { get; private set; }
    public string? LastFeedUrl { get; private set; }

    public Func<string, CancellationToken, Task<IReadOnlyList<RssFeedItem>>>?
        ReadHandler { get; set; }

    public Task<IReadOnlyList<RssFeedItem>> ReadAsync(
        string feedUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CallCount++;
        LastFeedUrl = feedUrl;

        return ReadHandler is { } handler
            ? handler(feedUrl, cancellationToken)
            : Task.FromResult<IReadOnlyList<RssFeedItem>>(items);
    }
}