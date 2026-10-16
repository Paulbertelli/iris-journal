namespace Iris.Application.UseCases.Articles;

public sealed class ArticleListItem
{
    public Guid Id { get; init; }
    public Guid RssSourceId { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? SourceLogoUrl { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ArticleUrl { get; init; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; init; }
    public DateTimeOffset CollectedAt { get; init; }
    
    public string? ImageUrl { get; init; }
}