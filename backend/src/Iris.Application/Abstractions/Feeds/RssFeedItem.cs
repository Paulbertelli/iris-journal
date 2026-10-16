namespace Iris.Application.Abstractions.Feeds;

public sealed record RssFeedItem
{
    public string Title { get; init; } = string.Empty;
    public string ArticleUrl { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}