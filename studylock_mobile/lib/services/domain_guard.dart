class DomainGuard {
  // Common OAuth providers necessary for study portal authentication
  static const List<String> oauthWhitelist = [
    'accounts.google.com',
    'login.microsoftonline.com',
    'appleid.apple.com',
    'github.com/login',
    'auth0.com',
    'okta.com',
  ];

  static bool isUrlAllowed(String url, String targetBaseUrl, {bool allowOAuth = true}) {
    if (url.isEmpty) return false;

    final lowerUrl = url.toLowerCase().trim();
    if (lowerUrl.startsWith('data:') ||
        lowerUrl.startsWith('blob:') ||
        lowerUrl.startsWith('about:')) {
      return true;
    }

    final uri = Uri.tryParse(url);
    final baseUri = Uri.tryParse(targetBaseUrl);

    if (uri == null || baseUri == null) return false;

    // Only allow http and https
    if (uri.scheme != 'http' && uri.scheme != 'https') {
      return false;
    }

    final host = uri.host.toLowerCase();
    final baseHost = baseUri.host.toLowerCase();

    // Match exact host or subdomain (e.g. video.khanacademy.org vs khanacademy.org)
    if (host == baseHost || host.endsWith('.$baseHost')) {
      return true;
    }

    // Check OAuth whitelist
    if (allowOAuth) {
      for (final oauthHost in oauthWhitelist) {
        if (host == oauthHost || host.endsWith('.$oauthHost')) {
          return true;
        }
      }
    }

    return false;
  }

  static String normalizeUrl(String input) {
    var trimmed = input.trim();
    if (trimmed.isEmpty) return '';

    if (!trimmed.startsWith('http://') && !trimmed.startsWith('https://')) {
      trimmed = 'https://$trimmed';
    }

    return trimmed;
  }
}
