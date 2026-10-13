using Iris.Domain.Entities;

namespace Iris.Domain.Tests;

public class RssSourceTests
{
    [Fact]
    public void Constructor_WithValidData_NormalizesValues()
    {
        var source = new RssSource(
            "  Le Monde  ",
            "https://example.com/feed",
            " fr ");

        Assert.NotEqual(Guid.Empty, source.Id);
        Assert.Equal("Le Monde", source.Name);
        Assert.Equal("https://example.com/feed", source.FeedUrl);
        Assert.Equal("FR", source.CountryCode);
        Assert.True(source.IsEnabled);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithBlankName_ThrowsArgumentException(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new RssSource(name, "https://example.com/feed", "FR"));
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("ftp://example.com/feed")]
    public void Constructor_WithInvalidFeedUrl_ThrowsArgumentException(string feedUrl)
    {
        Assert.Throws<ArgumentException>(() =>
            new RssSource("Le Monde", feedUrl, "FR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("F@")]
    public void Constructor_WithInvalidCountryCode_ThrowsArgumentException(string countryCode)
    {
        Assert.Throws<ArgumentException>(() =>
            new RssSource("Le Monde", "https://example.com/feed", countryCode));
    }
}