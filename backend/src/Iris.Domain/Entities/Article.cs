namespace Iris.Domain.Entities;

public sealed class Article
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RssSourceId { get; private set; }
    public string Title { get; private set; }
    public string ArticleUrl { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CollectedAt { get; private set; } = DateTimeOffset.UtcNow;

    public Article( Guid rssSourceId, string title, string articleUrl, DateTimeOffset? publishedAt = null)
    {
        if (rssSourceId == Guid.Empty)
            throw new ArgumentException("La source RSS est obligatoire.", nameof(rssSourceId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Le titre est obligatoire.", nameof(title));
     
        if (!Uri.TryCreate(articleUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("L’URL doit être une adresse HTTP ou HTTPS absolue.", nameof(articleUrl));
        
        Title = title.Trim();
        ArticleUrl = uri.AbsoluteUri;
        PublishedAt = publishedAt?.ToUniversalTime();
        RssSourceId = rssSourceId;
    }
}