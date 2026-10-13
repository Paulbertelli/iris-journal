namespace Iris.Domain.Entities;

public sealed class RssSource
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string FeedUrl { get; private set; }
    public string CountryCode { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    public RssSource(string name, string feedUrl, string countryCode, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom de la source RSS est obligatoire.", nameof(name));

        if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("L’URL doit être une adresse HTTP ou HTTPS absolue.", nameof(feedUrl));
        
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Le code pays est obligatoire.", nameof(countryCode));

        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();

        if (normalizedCountryCode.Length != 2 ||
            normalizedCountryCode.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Le code pays doit contenir deux lettres, par exemple FR.",
                nameof(countryCode));
        }
        
        Name = name.Trim();
        FeedUrl = uri.AbsoluteUri;
        CountryCode = normalizedCountryCode;
        IsEnabled = isEnabled;
    }
}