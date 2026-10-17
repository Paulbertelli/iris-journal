using System.Globalization;
using System.Xml.Linq;
using Iris.Application.Abstractions.Feeds;

namespace Iris.Infrastructure.Feeds;

public sealed class RssFeedReader(HttpClient httpClient) : IRssFeedReader
{
    public async Task<IReadOnlyList<RssFeedItem>> ReadAsync(
        string feedUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(feedUrl, cancellationToken);
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            cancellationToken.ThrowIfCancellationRequested();

            var document = XDocument.Parse(content);

            XNamespace dcDates = "http://purl.org/dc/elements/1.1/";
            XNamespace atomDates = "http://www.w3.org/2005/Atom";
            XNamespace rss10 = "http://purl.org/rss/1.0/";

            var dateNames = new HashSet<XName>
            {
                "pubDate",
                "lastBuildDate",
                dcDates + "date",
                atomDates + "published",
                atomDates + "updated",
                rss10 + "pubDate"
            };

            foreach (var dateElement in document.Descendants()
                         .Where(element => dateNames.Contains(element.Name))
                         .ToList())
            {
                if (DateTimeOffset.TryParse(
                        dateElement.Value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces
                        | DateTimeStyles.AssumeUniversal,
                        out var date))
                {
                    dateElement.Value = date.ToUniversalTime()
                        .ToString("r", CultureInfo.InvariantCulture);
                }
                else
                {
                    dateElement.Remove();
                }
            }

            var feed = CodeHollow.FeedReader.FeedReader.ReadFromString(
                document.ToString(SaveOptions.DisableFormatting));

            var items = new List<RssFeedItem>();
            
            XNamespace dc = "http://purl.org/dc/elements/1.1/";
            XNamespace atom = "http://www.w3.org/2005/Atom";

            foreach (var item in feed.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var title = item.Title?.Trim();
                
                if (string.IsNullOrWhiteSpace(title))
                    continue;
                
                if (!Uri.TryCreate(item.Link?.Trim(), UriKind.Absolute, out var uri)
                    || uri.Scheme != Uri.UriSchemeHttp
                        && uri.Scheme != Uri.UriSchemeHttps)
                    continue;

                var element = item.SpecificItem.Element;
                
                var dateValue = element.Element("pubDate")?.Value
                                ?? element.Element(dc + "date")?.Value
                                ?? element.Element(atom + "published")?.Value
                                ?? item.PublishingDateString;
                
                DateTimeOffset? publishedAt = null;

                if (DateTimeOffset.TryParse(
                        dateValue,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces
                        | DateTimeStyles.AssumeUniversal,
                        out var parsedDate))
                {
                    publishedAt = parsedDate.ToUniversalTime();
                }

                items.Add(new RssFeedItem
                {
                    Title = title,
                    ArticleUrl = uri.AbsoluteUri,
                    PublishedAt = publishedAt,
                    ImageUrl = ExtractImageUrl(element)
                });
            }

            return items;
        }
        catch (HttpRequestException exception)
        {
            throw new FeedReadException("Le téléchargement du flux a échoué.", exception);
        }
        catch (System.Xml.XmlException exception)
        {
            throw new FeedReadException("Le flux contient un XML invalide.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FeedReadException("Le délai de lecture du flux est dépassé.", exception, true);
        }
    }

    private static string? ExtractImageUrl(XElement item)
    {
        XNamespace media = "http://search.yahoo.com/mrss/";

        var candidates = item.Descendants(media + "content")
            .Where(element =>
            {
                var medium = (string?)element.Attribute("medium");
                var type = (string?)element.Attribute("type");

                if (string.Equals(medium, "image", StringComparison.OrdinalIgnoreCase)
                    || IsImageType(type))
                    return true;

                // Déduire le format de l’URL seulement sans métadonnées explicites.
                return string.IsNullOrWhiteSpace(medium)
                       && string.IsNullOrWhiteSpace(type)
                       && HasImageExtension((string?)element.Attribute("url"));
            })
            .Select(element => (string?)element.Attribute("url"))
            .Concat(
                item.Elements("enclosure")
                    .Where(element =>
                        IsImageType((string?)element.Attribute("type")))
                    .Select(element => (string?)element.Attribute("url")))
            .Concat(
                item.Descendants(media + "thumbnail")
                    .Select(element => (string?)element.Attribute("url")));

        foreach (var candidate in candidates)
        {
            if (Uri.TryCreate(candidate?.Trim(), UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri.AbsoluteUri;
            }
        }

        return null;
    }

    private static bool IsImageType(string? contentType)
    {
        return contentType?.Trim().StartsWith(
            "image/",
            StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool HasImageExtension(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri))
            return false;

        return Path.GetExtension(uri.AbsolutePath).ToLowerInvariant()
            is ".jpg" or ".jpeg" or ".png" or ".webp"
            or ".gif" or ".avif";
    }
}