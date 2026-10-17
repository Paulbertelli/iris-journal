using System.Net;
using System.Text;
using System.Xml;
using Iris.Application.Abstractions.Feeds;
using Iris.Infrastructure.Feeds;

namespace Iris.Infrastructure.Tests.FeedsTests;

public class RssFeedReaderTests
{
    private sealed class StubHttpMessageHandler(string xml)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    xml, Encoding.UTF8, "application/rss+xml")
            });
        }
    }

    private sealed class ThrowingHttpMessageHandler(
        Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }

    private sealed class CancelingHttpMessageHandler(
        CancellationTokenSource cancellation) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellation.Cancel();

            return Task.FromCanceled<HttpResponseMessage>(
                cancellationToken);
        }
    }

    [Fact]
    public async Task ReadAsync_WithValidRss_ReturnsItemWithUtcDate()
    {
        var xml = """
                  <rss version="2.0">
                    <channel>
                      <title>Flux test</title>
                      <item>
                        <title> Article test </title>
                        <link>https://example.com/article</link>
                        <pubDate>Wed, 14 Oct 2026 18:00:00 +0200</pubDate>
                      </item>
                    </channel>
                  </rss>
                  """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var result = await reader.ReadAsync("https://example.com/feed");

        var item = Assert.Single(result);

        Assert.Equal("Article test", item.Title);
        Assert.Equal("https://example.com/article", item.ArticleUrl);
        Assert.NotNull(item.PublishedAt);

        Assert.Equal(new DateTimeOffset(2026, 10, 14, 16, 00, 00, TimeSpan.Zero), item.PublishedAt.Value);

        Assert.Equal(TimeSpan.Zero, item.PublishedAt.Value.Offset);
    }

    [Fact]
    public async Task ReadAsync_WithInvalidItems_SkipsThem()
    {
        var xml = """
                  <rss version="2.0">
                    <channel>
                      <item>
                        <title>Article valide</title>
                        <link>https://example.com/valid</link>
                      </item>
                      <item>
                        <title>   </title>
                        <link>https://example.com/empty-title</link>
                      </item>
                      <item>
                        <title>Lien absent</title>
                      </item>
                      <item>
                        <title>Lien FTP</title>
                        <link>ftp://example.com/article</link>
                      </item>
                    </channel>
                  </rss>
                  """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var result = await reader.ReadAsync("https://example.com/feed");

        var item = Assert.Single(result);
        Assert.Equal("Article valide", item.Title);
        Assert.Equal("https://example.com/valid", item.ArticleUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("date-invalide")]
    public async Task ReadAsync_WithMissingOrInvalidDate_ReturnsNullDate(
        string? pubDate)
    {
        var dateElement = pubDate is null
            ? string.Empty
            : $"<pubDate>{pubDate}</pubDate>";

        var xml = $"""
                   <rss version="2.0">
                     <channel>
                       <item>
                         <title>Article test</title>
                         <link>https://example.com/article</link>
                         {dateElement}
                       </item>
                     </channel>
                   </rss>
                   """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var result = await reader.ReadAsync("https://example.com/feed");

        var item = Assert.Single(result);
        Assert.Equal("Article test", item.Title);
        Assert.Null(item.PublishedAt);
    }

    [Fact]
    public async Task ReadAsync_WithEmptyChannel_ReturnsEmptyList()
    {
        var xml = """
                  <rss version="2.0">
                    <channel />
                  </rss>
                  """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var result = await reader.ReadAsync("https://example.com/feed");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReadAsync_WithInvalidXml_ThrowsFeedReadException()
    {
        using var client = new HttpClient(
            new StubHttpMessageHandler("<rss>"));

        var reader = new RssFeedReader(client);

        var exception = await Assert.ThrowsAsync<FeedReadException>(
            () => reader.ReadAsync("https://example.com/feed"));

        Assert.False(exception.IsTimeout);
        Assert.IsType<XmlException>(exception.InnerException);
    }

    [Fact]
    public async Task ReadAsync_WhenRequestTimesOut_ThrowsTimeoutFeedReadException()
    {
        using var client = new HttpClient(
            new ThrowingHttpMessageHandler(
                new TaskCanceledException("Délai dépassé")));

        var reader = new RssFeedReader(client);

        var exception = await Assert.ThrowsAsync<FeedReadException>(
            () => reader.ReadAsync("https://example.com/feed"));

        Assert.True(exception.IsTimeout);
    }

    [Fact]
    public async Task ReadAsync_WhenCallerCancels_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();

        using var client = new HttpClient(
            new CancelingHttpMessageHandler(cancellation));

        var reader = new RssFeedReader(client);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => reader.ReadAsync(
                "https://example.com/feed",
                cancellation.Token));
    }

    [Theory]
    [InlineData(
        """<media:content url="https://example.com/photo.jpg" width="644" height="322" />""",
        "https://example.com/photo.jpg")]
    [InlineData(
        """<media:content url="https://example.com/photo.jpg" medium="image" />""",
        "https://example.com/photo.jpg")]
    [InlineData(
        """<media:content url="https://example.com/photo.png" type="image/png" />""",
        "https://example.com/photo.png")]
    [InlineData(
        """<enclosure url="https://example.com/photo.jpg" type="image/jpeg" length="123" />""",
        "https://example.com/photo.jpg")]
    [InlineData(
        """<media:thumbnail url="https://example.com/thumb.jpg" />""",
        "https://example.com/thumb.jpg")]
    [InlineData("", null)]
    [InlineData(
        """<media:content url="https://example.com/video.mp4" medium="video" />""",
        null)]
    [InlineData(
        """<media:thumbnail url="ftp://example.com/photo.jpg" />""",
        null)]
    [InlineData(
        """
        <media:content url="adresse-invalide" medium="image" />
        <media:thumbnail url="https://example.com/fallback.jpg" />
        """,
        "https://example.com/fallback.jpg")]
    [InlineData(
        """
        <media:thumbnail url="https://example.com/thumb.jpg" />
        <media:content url="https://example.com/full.jpg" medium="image" />
        """,
        "https://example.com/full.jpg")]
    public async Task ReadAsync_WithImageMetadata_ReturnsExpectedImageUrl(
        string imageMetadata,
        string? expectedImageUrl)
    {
        var xml = $"""
                   <rss version="2.0" xmlns:media="http://search.yahoo.com/mrss/">
                     <channel>
                       <item>
                         <title>Article test</title>
                         <link>https://example.com/article</link>
                         {imageMetadata}
                       </item>
                     </channel>
                   </rss>
                   """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var result = await reader.ReadAsync("https://example.com/feed");

        var item = Assert.Single(result);

        Assert.Equal(expectedImageUrl, item.ImageUrl);
    }

    [Theory]
    [InlineData("EDT", 14)]
    [InlineData("EST", 15)]
    [InlineData("GMT", 10)]
    public async Task ReadAsync_WithNamedTimeZone_ReturnsUtcDate(
        string timeZone,
        int expectedHour)
    {
        var xml = $"""
                   <rss version="2.0">
                     <channel>
                       <title>Flux test</title>
                       <link>https://example.com</link>
                       <description>Test</description>
                       <item>
                         <title>Article test</title>
                         <link>https://example.com/article</link>
                         <pubDate>Sat, 17 Oct 2026 10:19:35 {timeZone}</pubDate>
                       </item>
                     </channel>
                   </rss>
                   """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var items = await reader.ReadAsync("https://example.com/feed");

        var item = Assert.Single(items);
        var date = Assert.IsType<DateTimeOffset>(item.PublishedAt);

        Assert.Equal(
            new DateTimeOffset(
                2026, 10, 17, expectedHour, 19, 35, TimeSpan.Zero),
            date);

        Assert.Equal(TimeSpan.Zero, date.Offset);
    }

    [Fact]
    public async Task ReadAsync_WithHtmlDescription_ExtractsImageAndDecodesUrl()
    {
        var xml = """
                  <rss version="2.0">
                    <channel>
                      <title>Flux test</title>
                      <link>https://example.com</link>
                      <description>Test</description>
                      <item>
                        <title>Article test</title>
                        <link>https://example.com/article</link>
                        <description><![CDATA[
                          <img src='https://example.com/photo.jpg?width=620&amp;height=349'>
                          <p>Description de l’article.</p>
                        ]]></description>
                      </item>
                    </channel>
                  </rss>
                  """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var items = await reader.ReadAsync("https://example.com/feed");

        Assert.Equal(
            "https://example.com/photo.jpg?width=620&height=349",
            Assert.Single(items).ImageUrl);
    }

    [Fact]
    public async Task ReadAsync_WithMediaAndHtmlImages_PrefersMediaImage()
    {
        var xml = """
                  <rss version="2.0"
                       xmlns:media="http://search.yahoo.com/mrss/">
                    <channel>
                      <title>Flux test</title>
                      <link>https://example.com</link>
                      <description>Test</description>
                      <item>
                        <title>Article test</title>
                        <link>https://example.com/article</link>
                        <media:content
                            medium="image"
                            url="https://example.com/media.jpg" />
                        <description><![CDATA[
                          <img src="https://example.com/html.jpg">
                        ]]></description>
                      </item>
                    </channel>
                  </rss>
                  """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var items = await reader.ReadAsync("https://example.com/feed");

        Assert.Equal(
            "https://example.com/media.jpg",
            Assert.Single(items).ImageUrl);
    }

    [Fact]
    public async Task ReadAsync_WithInvalidHtmlImageUrl_SkipsIt()
    {
        var xml = """
                  <rss version="2.0">
                    <channel>
                      <title>Flux test</title>
                      <link>https://example.com</link>
                      <description>Test</description>
                      <item>
                        <title>Article test</title>
                        <link>https://example.com/article</link>
                        <description><![CDATA[
                          <img src="javascript:alert(1)">
                          <img src="https://example.com/valid.jpg">
                        ]]></description>
                      </item>
                    </channel>
                  </rss>
                  """;

        using var client = new HttpClient(new StubHttpMessageHandler(xml));
        var reader = new RssFeedReader(client);

        var items = await reader.ReadAsync("https://example.com/feed");

        Assert.Equal(
            "https://example.com/valid.jpg",
            Assert.Single(items).ImageUrl);
    }
}