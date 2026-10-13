namespace Iris.Domain.Entities;

public sealed class NewsEvent
{
    private readonly List<Article> _articles = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; }
    public NewsSection GeographicSection { get; private set; }
    public int? ImportanceScore { get; private set; }

    public IReadOnlyCollection<Article> Articles => _articles.AsReadOnly();

    public NewsEvent(
        string title,
        NewsSection geographicSection = NewsSection.Unclassified)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Le titre de l’événement est obligatoire.",
                nameof(title));
        }

        EnsureValidSection(geographicSection);

        Title = title.Trim();
        GeographicSection = geographicSection;
    }

    public void AddArticle(Article article)
    {
        ArgumentNullException.ThrowIfNull(article);

        if (_articles.Any(existing => existing.Id == article.Id))
        {
            throw new InvalidOperationException(
                "Cet article est déjà rattaché à l’événement.");
        }

        _articles.Add(article);
    }

    public void SetGeographicSection(NewsSection section)
    {
        EnsureValidSection(section);
        GeographicSection = section;
    }

    public void SetImportanceScore(int score)
    {
        if (score is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(score),
                "Le score d’importance doit être compris entre 0 et 10.");
        }

        ImportanceScore = score;
    }

    private static void EnsureValidSection(NewsSection section)
    {
        if (!Enum.IsDefined(section))
        {
            throw new ArgumentOutOfRangeException(
                nameof(section),
                "La section géographique n’est pas valide.");
        }
    }
}