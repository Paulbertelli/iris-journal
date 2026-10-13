using Iris.Domain.Entities;
using Iris.Domain.Enums;

namespace Iris.Domain.Tests;

public class NewsEventTests
{
    [Fact]
    public void Constructor_WithValidTitle_TrimsTitleAndDefaultsSection()
    {
        var newsEvent = new NewsEvent("  Titre  ");

        Assert.NotEqual(Guid.Empty, newsEvent.Id);
        Assert.Equal("Titre", newsEvent.Title);
        Assert.Equal(NewsSection.Unclassified, newsEvent.GeographicSection);
        Assert.Null(newsEvent.ImportanceScore);
        Assert.Empty(newsEvent.Articles);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTitle_ThrowsArgumentException(string title)
    {
        Assert.Throws<ArgumentException>(() => new NewsEvent(title));
    }

    [Fact]
    public void AddArticle_WithSameArticleTwice_ThrowsInvalidOperationException()
    {
        var newsEvent = new NewsEvent("Titre");
        var article = new Article(
            Guid.NewGuid(),
            "Article",
            "https://example.com/article");

        newsEvent.AddArticle(article);

        Assert.Throws<InvalidOperationException>(
            () => newsEvent.AddArticle(article));

        Assert.Single(newsEvent.Articles);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void SetImportanceScore_WithScoreBetweenZeroAndTen_StoresScore(int score)
    {
        var newsEvent = new NewsEvent("Titre");

        newsEvent.SetImportanceScore(score);

        Assert.Equal(score, newsEvent.ImportanceScore);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void SetImportanceScore_WithScoreOutsideRange_Throws(int score)
    {
        var newsEvent = new NewsEvent("Titre");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => newsEvent.SetImportanceScore(score));
    }

    [Fact]
    public void Constructor_WithUnknownSection_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new NewsEvent("Titre", (NewsSection)999));
    }
}