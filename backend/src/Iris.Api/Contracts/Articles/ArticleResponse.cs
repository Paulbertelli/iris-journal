namespace Iris.Api.Contracts.Articles;

public sealed class ArticleResponse
{
    public Guid Id { get; init; }
    public Guid RssSourceId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ArticleUrl { get; init; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset CollectedAt { get; init; }
}