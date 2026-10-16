
namespace Iris.Domain.Entities;

public sealed class RssSource
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string FeedUrl { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public string? Description { get; private set; }
    public string? LogoUrl { get; private set; }
    
    public RssSource(
        string name, 
        string feedUrl, 
        string countryCode, 
        bool isEnabled = true,
        string? description = null, 
        string? logoUrl = null)
    {
        Update(name, feedUrl, countryCode, isEnabled, description, logoUrl);
    }

    public void Update(string name,
        string feedUrl,
        string countryCode,
        bool isEnabled,
        string? description,
        string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Le nom de la source RSS est obligatoire.", nameof(name));

        var normalizedFeedUrl = NormalizeHttpUrl(feedUrl, nameof(feedUrl));
        
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Le code pays est obligatoire.", nameof(countryCode));

        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();

        if (normalizedCountryCode.Length != 2 || normalizedCountryCode.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Le code pays doit contenir deux lettres, par exemple FR.", nameof(countryCode));
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        
        var normalizeLogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : NormalizeHttpUrl(logoUrl, nameof(logoUrl));
        
        Name = name.Trim();
        FeedUrl = normalizedFeedUrl;
        CountryCode = normalizedCountryCode;
        Description = normalizedDescription;
        LogoUrl = normalizeLogoUrl;
        IsEnabled = isEnabled;
    }
    
    private static string NormalizeHttpUrl(string value, string parameterName)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("L’URL doit être une adresse HTTP ou HTTPS absolue.", parameterName);
        }

        return uri.AbsoluteUri;
    }
}