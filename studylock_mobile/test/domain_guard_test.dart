import 'package:flutter_test/flutter_test.dart';
import 'package:studylock_mobile/services/domain_guard.dart';

void main() {
  group('DomainGuard Unit Tests', () {
    test('Normalizes naked URLs with https', () {
      expect(DomainGuard.normalizeUrl('khanacademy.org'), 'https://khanacademy.org');
      expect(DomainGuard.normalizeUrl('https://madrasati.sa'), 'https://madrasati.sa');
      expect(DomainGuard.normalizeUrl('   http://example.com/page  '), 'http://example.com/page');
    });

    test('Allows target origin and its subdomains', () {
      const base = 'https://khanacademy.org';
      expect(DomainGuard.isUrlAllowed('https://khanacademy.org/math', base), isTrue);
      expect(DomainGuard.isUrlAllowed('https://video.khanacademy.org/embed/123', base), isTrue);
      expect(DomainGuard.isUrlAllowed('https://api.khanacademy.org/v1/user', base), isTrue);
    });

    test('Rejects external unapproved origins', () {
      const base = 'https://khanacademy.org';
      expect(DomainGuard.isUrlAllowed('https://tiktok.com', base), isFalse);
      expect(DomainGuard.isUrlAllowed('https://instagram.com/p/123', base), isFalse);
      expect(DomainGuard.isUrlAllowed('https://youtube.com', base), isFalse);
    });

    test('Allows OAuth login endpoints when allowOAuth is true', () {
      const base = 'https://portal.education.org';
      expect(DomainGuard.isUrlAllowed('https://accounts.google.com/o/oauth2/auth', base, allowOAuth: true), isTrue);
      expect(DomainGuard.isUrlAllowed('https://login.microsoftonline.com/common/oauth2', base, allowOAuth: true), isTrue);
      expect(DomainGuard.isUrlAllowed('https://accounts.google.com/o/oauth2/auth', base, allowOAuth: false), isFalse);
    });

    test('Allows data, blob, and about schemes for internal players', () {
      const base = 'https://khanacademy.org';
      expect(DomainGuard.isUrlAllowed('data:image/png;base64,iVBOR', base), isTrue);
      expect(DomainGuard.isUrlAllowed('blob:https://khanacademy.org/uuid', base), isTrue);
      expect(DomainGuard.isUrlAllowed('about:blank', base), isTrue);
    });
  });
}
