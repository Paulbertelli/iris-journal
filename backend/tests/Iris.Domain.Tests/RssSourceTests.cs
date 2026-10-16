using Iris.Domain.Entities;

namespace Iris.Domain.Tests;

public class RssSourceTests
{
    [Fact]
    public void Constructor_WithValidData_NormalizesValues()
    {
        var source = new RssSource(
            "  Le Monde  ",
            " https://example.com/feed ",
            " fr ",
            description: " Description test ",
            logoUrl: " https://example.com/logo%20test.png ");

        Assert.NotEqual(Guid.Empty, source.Id);
        Assert.Equal("Le Monde", source.Name);
        Assert.Equal("https://example.com/feed", source.FeedUrl);
        Assert.Equal("FR", source.CountryCode);
        Assert.True(source.IsEnabled);
        Assert.Equal("Description test", source.Description);
        Assert.Equal("https://example.com/logo%20test.png", source.LogoUrl);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankOptionalDetails_StoresNull(string? value)
    {
        var source = new RssSource(
            "Source test",
            "https://example.com/feed",
            "FR",
            description: value,
            logoUrl: value);

        Assert.Null(source.Description);
        Assert.Null(source.LogoUrl);
    }

    [Fact]
    public void Update_WithValidData_UpdatesDetailsAndPreservesId()
    {
        var source = new RssSource(
            "Ancienne source",
            "https://example.com/old",
            "FR");

        var originalId = source.Id;

        source.Update(
            " Nouvelle source ",
            " https://example.com/new ",
            " gb ",
            false,
            " Description ",
            " https://example.com/logo.png ");

        Assert.Equal(originalId, source.Id);
        Assert.Equal("Nouvelle source", source.Name);
        Assert.Equal("https://example.com/new", source.FeedUrl);
        Assert.Equal("GB", source.CountryCode);
        Assert.False(source.IsEnabled);
        Assert.Equal("Description", source.Description);
        Assert.Equal("https://example.com/logo.png", source.LogoUrl);
    }

    [Fact]
    public void Update_WithNullDetails_ClearsDescriptionAndLogo()
    {
        var source = new RssSource(
            "Source test",
            "https://example.com/feed",
            "FR",
            description: "Description",
            logoUrl: "https://example.com/logo.png");

        source.Update(
            source.Name,
            source.FeedUrl,
            source.CountryCode,
            source.IsEnabled,
            null,
            null);

        Assert.Null(source.Description);
        Assert.Null(source.LogoUrl);
    }

    [Theory]
    [InlineData("relative/logo.png")]
    [InlineData("ftp://example.com/logo.png")]
    public void Update_WithInvalidLogo_ThrowsAndPreservesAllValues(string logoUrl)
    {
        var source = new RssSource(
            "Source initiale",
            "https://example.com/feed",
            "FR",
            description: "Description initiale",
            logoUrl: "https://example.com/logo.png");

        var originalId = source.Id;

        var exception = Assert.Throws<ArgumentException>(() =>
            source.Update(
                "Nouveau nom",
                "https://example.com/new",
                "GB",
                false,
                "Nouvelle description",
                logoUrl));

        Assert.Equal("logoUrl", exception.ParamName);
        Assert.Equal(originalId, source.Id);
        Assert.Equal("Source initiale", source.Name);
        Assert.Equal("https://example.com/feed", source.FeedUrl);
        Assert.Equal("FR", source.CountryCode);
        Assert.True(source.IsEnabled);
        Assert.Equal("Description initiale", source.Description);
        Assert.Equal("https://example.com/logo.png", source.LogoUrl);
    }
}