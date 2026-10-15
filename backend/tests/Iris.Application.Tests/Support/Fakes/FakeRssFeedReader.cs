using Iris.Application.Abstractions.Feeds;

namespace Iris.Application.Tests.Support.Fakes;

public sealed class FakeRssFeedReader(
    params RssFeedItem[] items) : IRssFeedReader
{
    public int CallCount { get; private set; }
    public string? LastFeedUrl { get; private set; }

    public Task<IReadOnlyList<RssFeedItem>> ReadAsync(
        string feedUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CallCount++;
        LastFeedUrl = feedUrl;

        return Task.FromResult<IReadOnlyList<RssFeedItem>>(items);
    }
}