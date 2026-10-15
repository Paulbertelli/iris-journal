using Iris.Domain.Entities;

namespace Iris.Domain.Tests;

public class ArticleTests
{
    [Fact]
    public void Constructor_WithValidData_NormalizesArticleValues()
    {
        var sourceId = Guid.NewGuid();
        var publishedAt = new DateTimeOffset(
            2026, 10, 13, 7, 0, 0, TimeSpan.FromHours(2));

        var article = new Article(
            sourceId,
            "  Titre de l’article  ",
            "https://example.com/article",
            publishedAt);

        Assert.NotEqual(Guid.Empty, article.Id);
        Assert.Equal(sourceId, article.RssSourceId);
        Assert.Equal("Titre de l’article", article.Title);
        Assert.Equal("https://example.com/article", article.ArticleUrl);
        Assert.Equal(publishedAt.ToUniversalTime(), article.PublishedAt);
        Assert.Equal(TimeSpan.Zero, article.CollectedAt.Offset);
    }

    [Fact]
    public void Constructor_WithEmptySourceId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Article(Guid.Empty, "Titre", "https://example.com/article"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_ThrowsArgumentException(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            new Article(Guid.NewGuid(), title, "https://example.com/article"));
    }

    [Theory]
    [InlineData("/article")]
    [InlineData("ftp://example.com/article")]
    public void Constructor_WithNonHttpUrl_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() =>
            new Article(Guid.NewGuid(), "Titre", url));
    }
}