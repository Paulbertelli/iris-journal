using System.Globalization;
using System.Xml.Linq;
using HtmlAgilityPack;
using Iris.Application.Abstractions.Feeds;

namespace Iris.Infrastructure.Feeds;

public sealed class RssFeedReader(HttpClient httpClient) : IRssFeedReader
{
    private static readonly XNamespace Dc =
        "http://purl.org/dc/elements/1.1/";

    private static readonly XNamespace Atom =
        "http://www.w3.org/2005/Atom";

    private static readonly XNamespace Media =
        "http://search.yahoo.com/mrss/";

    private static readonly XNamespace Content =
        "http://purl.org/rss/1.0/modules/content/";

    private static readonly IReadOnlyDictionary<string, string> RssTimeZones =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["UT"] = "+00:00",
            ["GMT"] = "+00:00",
            ["UTC"] = "+00:00",
            ["EST"] = "-05:00",
            ["EDT"] = "-04:00",
            ["CST"] = "-06:00",
            ["CDT"] = "-05:00",
            ["MST"] = "-07:00",
            ["MDT"] = "-06:00",
            ["PST"] = "-08:00",
            ["PDT"] = "-07:00"
        };

    public async Task<IReadOnlyList<RssFeedItem>> ReadAsync(
        string feedUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                feedUrl, cancellationToken);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var document = XDocument.Parse(content);

            NormalizeDates(document, cancellationToken);

            var feed = CodeHollow.FeedReader.FeedReader.ReadFromString(
                document.ToString(SaveOptions.DisableFormatting));

            var items = new List<RssFeedItem>();

            foreach (var item in feed.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = item.Title?.Trim();

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                var articleUrl = NormalizeHttpUrl(item.Link);

                if (articleUrl is null)
                    continue;

                var element = item.SpecificItem.Element;

                var dateValue = element.Element("pubDate")?.Value
                                ?? element.Element(Dc + "date")?.Value
                                ?? element.Element(Atom + "published")?.Value
                                ?? item.PublishingDateString;

                DateTimeOffset? publishedAt =
                    TryParseFeedDate(dateValue, out var date)
                        ? date.ToUniversalTime()
                        : null;

                items.Add(new RssFeedItem
                {
                    Title = title,
                    ArticleUrl = articleUrl,
                    PublishedAt = publishedAt,
                    ImageUrl = ExtractImageUrl(element)
                });
            }

            return items;
        }
        catch (HttpRequestException exception)
        {
            throw new FeedReadException(
                "Le téléchargement du flux a échoué.", exception);
        }
        catch (System.Xml.XmlException exception)
        {
            throw new FeedReadException(
                "Le flux contient un XML invalide.", exception);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new FeedReadException(
                "Le délai de lecture du flux est dépassé.",
                exception,
                true);
        }
    }

    private static void NormalizeDates(
        XDocument document,
        CancellationToken cancellationToken)
    {
        XNamespace rss10 = "http://purl.org/rss/1.0/";

        var dateNames = new HashSet<XName>
        {
            "pubDate",
            "lastBuildDate",
            Dc + "date",
            Atom + "published",
            Atom + "updated",
            rss10 + "pubDate"
        };

        var elements = document.Descendants()
            .Where(element => dateNames.Contains(element.Name))
            .ToList();

        foreach (var element in elements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryParseFeedDate(element.Value, out var date))
            {
                element.Remove();
                continue;
            }

            // Atom et Dublin Core utilisent une date ISO 8601.
            var format = element.Name.Namespace == Atom
                         || element.Name.Namespace == Dc
                ? "O"
                : "r";

            element.Value = date.ToUniversalTime()
                .ToString(format, CultureInfo.InvariantCulture);
        }
    }

    private static bool TryParseFeedDate(
        string? value,
        out DateTimeOffset date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Trim();
        var separator = normalized.LastIndexOf(' ');

        if (separator >= 0)
        {
            var zone = normalized[(separator + 1)..];

            if (RssTimeZones.TryGetValue(zone, out var offset))
            {
                normalized = normalized[..(separator + 1)] + offset;
            }
        }

        return DateTimeOffset.TryParse(
            normalized,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces
            | DateTimeStyles.AssumeUniversal,
            out date);
    }

    private static string? ExtractImageUrl(XElement item)
    {
        var candidates = item.Descendants(Media + "content")
            .Where(element =>
            {
                var medium = (string?)element.Attribute("medium");
                var type = (string?)element.Attribute("type");

                if (string.Equals(
                        medium,
                        "image",
                        StringComparison.OrdinalIgnoreCase)
                    || IsImageType(type))
                {
                    return true;
                }

                return string.IsNullOrWhiteSpace(medium)
                       && string.IsNullOrWhiteSpace(type)
                       && HasImageExtension(
                           (string?)element.Attribute("url"));
            })
            .Select(element => (string?)element.Attribute("url"))
            .Concat(
                item.Elements("enclosure")
                    .Where(element =>
                        IsImageType((string?)element.Attribute("type")))
                    .Select(element =>
                        (string?)element.Attribute("url")))
            .Concat(
                item.Descendants(Media + "thumbnail")
                    .Select(element =>
                        (string?)element.Attribute("url")));

        foreach (var candidate in candidates)
        {
            var url = NormalizeHttpUrl(candidate);

            if (url is not null)
                return url;
        }

        // Le HTML sert uniquement de solution de repli.
        var htmlContents = new[]
        {
            item.Element("description")?.Value,
            item.Element(Content + "encoded")?.Value,
            item.Element(Atom + "summary")?.Value,
            item.Element(Atom + "content")?.Value
        };

        foreach (var html in htmlContents)
        {
            var url = ExtractHtmlImageUrl(html);

            if (url is not null)
                return url;
        }

        return null;
    }

    private static string? ExtractHtmlImageUrl(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        var document = new HtmlDocument();
        document.LoadHtml(html);

        foreach (var image in document.DocumentNode.Descendants("img"))
        {
            var source = image.GetAttributeValue("src", string.Empty);
            var url = NormalizeHttpUrl(HtmlEntity.DeEntitize(source));

            if (url is not null)
                return url;
        }

        return null;
    }

    private static string? NormalizeHttpUrl(string? value)
    {
        if (!Uri.TryCreate(
                value?.Trim(),
                UriKind.Absolute,
                out var uri))
        {
            return null;
        }

        return uri.Scheme == Uri.UriSchemeHttp
               || uri.Scheme == Uri.UriSchemeHttps
            ? uri.AbsoluteUri
            : null;
    }

    private static bool IsImageType(string? contentType)
    {
        return contentType?.Trim().StartsWith(
            "image/",
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool HasImageExtension(string? value)
    {
        if (!Uri.TryCreate(
                value?.Trim(),
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant()
            is ".jpg" or ".jpeg" or ".png" or ".webp"
            or ".gif" or ".avif";
    }
}