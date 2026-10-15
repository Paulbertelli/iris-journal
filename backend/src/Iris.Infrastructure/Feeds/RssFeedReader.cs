using System.Globalization;
using System.Xml.Linq;
using Iris.Application.Abstractions.Feeds;

namespace Iris.Infrastructure.Feeds;

public sealed class RssFeedReader(HttpClient httpClient) : IRssFeedReader
{
    public async Task<IReadOnlyList<RssFeedItem>> ReadAsync(string feedUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(feedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

            var items = new List<RssFeedItem>();

            foreach (var item in document.Descendants("item"))
            {
                var title = item.Element("title")?.Value.Trim();
                var link = item.Element("link")?.Value.Trim();
                var pubDate = item.Element("pubDate")?.Value;

                if (string.IsNullOrWhiteSpace(title))
                    continue;

                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    continue;

                DateTimeOffset? publishedAt = null;

                if (DateTimeOffset.TryParse(pubDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces,
                        out var parsedDate))
                    publishedAt = parsedDate.ToUniversalTime();

                items.Add(new RssFeedItem
                {
                    Title = title,
                    ArticleUrl = uri.AbsoluteUri,
                    PublishedAt = publishedAt
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
}