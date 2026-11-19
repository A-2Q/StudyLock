using System.IO;
using StudyLock.Models;

namespace StudyLock.Services;

public sealed class DomainGuardService
{
    private static readonly HashSet<string> TwoPartPublicSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "com.sa", "edu.sa", "gov.sa", "org.sa", "net.sa", "med.sa", "sch.sa",
        "co.uk", "org.uk", "ac.uk", "gov.uk",
        "com.au", "edu.au", "gov.au",
        "com.eg", "edu.eg", "gov.eg",
        "com.tr", "edu.tr", "gov.tr",
        "co.jp", "ne.jp", "or.jp",
        "com.br", "com.mx", "co.in"
    };

    private readonly string _allowedBaseDomain;
    private readonly SessionMode _mode;
    private readonly HashSet<string> _allowedPdfPaths;
    private readonly HashSet<string> _oauthWhitelist;

    public DomainGuardService(
        string primaryUrl,
        SessionMode mode,
        IEnumerable<string>? attachedPdfPaths = null,
        IEnumerable<string>? oauthWhitelistHosts = null)
    {
        _mode = mode;
        _allowedBaseDomain = ExtractRegistrableDomain(NormalizeUrl(primaryUrl));

        _allowedPdfPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (attachedPdfPaths is not null)
        {
            foreach (string path in attachedPdfPaths)
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    try
                    {
                        string fullPath = Path.GetFullPath(path);
                        if (fullPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            _allowedPdfPaths.Add(fullPath);
                        }
                    }
                    catch
                    {
                        // Ignore malformed local file paths
                    }
                }
            }
        }

        _oauthWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (mode == SessionMode.Study)
        {
            IEnumerable<string> hosts = oauthWhitelistHosts ?? AppSettings.GetDefaultOAuthWhitelist();
            foreach (string host in hosts)
            {
                string trimmed = host.Trim().TrimStart('.');
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    _oauthWhitelist.Add(trimmed);
                }
            }
        }
    }

    public string AllowedBaseDomain => _allowedBaseDomain;

    public static string NormalizeUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return string.Empty;
        }

        string trimmed = rawUrl.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "https://" + trimmed;
        }

        return trimmed;
    }

    public static bool TryValidateWebUrl(string rawUrl, out Uri? validatedUri)
    {
        validatedUri = null;
        string normalized = NormalizeUrl(rawUrl);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !uri.Host.Contains('.', StringComparison.Ordinal) && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        validatedUri = uri;
        return true;
    }

    public static string ExtractRegistrableDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return string.Empty;
        }

        string host = uri.Host.ToLowerInvariant().TrimEnd('.');
        if (host == "localhost" || System.Net.IPAddress.TryParse(host, out _))
        {
            return host;
        }

        string[] parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2)
        {
            return host;
        }

        string lastTwo = $"{parts[^2]}.{parts[^1]}";
        if (TwoPartPublicSuffixes.Contains(lastTwo) && parts.Length >= 3)
        {
            return $"{parts[^3]}.{lastTwo}";
        }

        return lastTwo;
    }

    public bool IsNavigationAllowed(string? candidateUrl)
    {
        if (string.IsNullOrWhiteSpace(candidateUrl))
        {
            return false;
        }

        if (candidateUrl.Equals("about:blank", StringComparison.OrdinalIgnoreCase) ||
            candidateUrl.StartsWith("blob:", StringComparison.OrdinalIgnoreCase) ||
            candidateUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(candidateUrl, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        // Local attached PDF files (Study Mode only)
        if (uri.IsFile)
        {
            if (_mode != SessionMode.Study)
            {
                return false;
            }

            try
            {
                string localPath = Path.GetFullPath(uri.LocalPath);
                return _allowedPdfPaths.Contains(localPath);
            }
            catch
            {
                return false;
            }
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        string host = uri.Host.ToLowerInvariant().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(_allowedBaseDomain))
        {
            return false;
        }

        // 1. Primary base domain and subdomains
        if (IsHostMatchingDomain(host, _allowedBaseDomain))
        {
            return true;
        }

        // 2. Special case: YouTube Kids occasionally redirects between youtubekids.com and kids.youtube.com
        if (_allowedBaseDomain.Equals("youtubekids.com", StringComparison.OrdinalIgnoreCase) &&
            host.Equals("kids.youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 3. Technical OAuth whitelist (Study Mode only)
        if (_mode == SessionMode.Study)
        {
            foreach (string allowedOAuthHost in _oauthWhitelist)
            {
                if (IsHostMatchingDomain(host, allowedOAuthHost))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsHostMatchingDomain(string host, string allowedDomain)
    {
        return host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith("." + allowedDomain, StringComparison.OrdinalIgnoreCase);
    }
}
