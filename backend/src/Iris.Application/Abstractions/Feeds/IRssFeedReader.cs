namespace Iris.Application.Abstractions.Feeds;

public interface IRssFeedReader
{
    Task<IReadOnlyList<RssFeedItem>> ReadAsync(
        string feedUrl,
        CancellationToken cancellationToken = default);
}