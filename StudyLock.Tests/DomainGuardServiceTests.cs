using System.IO;
using StudyLock.Models;
using StudyLock.Services;
using Xunit;

namespace StudyLock.Tests;

public sealed class DomainGuardServiceTests
{
    [Fact]
    public void StudyMode_AllowsPrimaryDomainSubdomainsAndOAuth_BlocksExternalAndSpoofed()
    {
        string pdfPath = Path.Combine(Path.GetTempPath(), "StudyLock_TestBook.pdf");
        DomainGuardService guard = new(
            "https://e-learning.qiyas.sa/courses/1",
            SessionMode.Study,
            attachedPdfPaths: [pdfPath]);

        Assert.Equal("qiyas.sa", guard.AllowedBaseDomain);

        // Allowed: primary domain & subdomains
        Assert.True(guard.IsNavigationAllowed("https://qiyas.sa"));
        Assert.True(guard.IsNavigationAllowed("https://e-learning.qiyas.sa/quiz/42"));
        Assert.True(guard.IsNavigationAllowed("https://portal.qiyas.sa/login"));

        // Allowed: technical OAuth login endpoints
        Assert.True(guard.IsNavigationAllowed("https://accounts.google.com/o/oauth2/v2/auth"));
        Assert.True(guard.IsNavigationAllowed("https://login.microsoftonline.com/common/oauth2"));

        // Allowed: user-attached local PDF only
        string pdfUri = new Uri(Path.GetFullPath(pdfPath)).AbsoluteUri;
        Assert.True(guard.IsNavigationAllowed(pdfUri));

        // Blocked: other local files & non-attached PDFs
        string otherFileUri = new Uri(Path.Combine(Path.GetTempPath(), "secret.pdf")).AbsoluteUri;
        Assert.False(guard.IsNavigationAllowed(otherFileUri));

        // Blocked: external sites & domain spoofing attacks
        Assert.False(guard.IsNavigationAllowed("https://www.google.com/search?q=games"));
        Assert.False(guard.IsNavigationAllowed("https://www.youtube.com/watch?v=123"));
        Assert.False(guard.IsNavigationAllowed("https://evil-qiyas.sa/phishing"));
        Assert.False(guard.IsNavigationAllowed("https://qiyas.sa.evil.com/fake"));
    }

    [Fact]
    public void ChildMode_StrictlyBlocksOAuthAndExternalLinks_AllowsYouTubeKidsRedirect()
    {
        DomainGuardService guard = new("https://www.youtubekids.com", SessionMode.Child);

        Assert.True(guard.IsNavigationAllowed("https://www.youtubekids.com/watch?v=abc"));
        Assert.True(guard.IsNavigationAllowed("https://kids.youtube.com/"));

        // Child mode MUST NOT allow OAuth or regular YouTube or local files
        Assert.False(guard.IsNavigationAllowed("https://accounts.google.com/signin"));
        Assert.False(guard.IsNavigationAllowed("https://www.youtube.com/"));
        Assert.False(guard.IsNavigationAllowed("file:///C:/Windows/win.ini"));
    }

    [Fact]
    public void ExtractRegistrableDomain_HandlesTwoPartPublicSuffixesCorrectly()
    {
        Assert.Equal("school.edu.sa", DomainGuardService.ExtractRegistrableDomain("https://lms.school.edu.sa/home"));
        Assert.Equal("bbc.co.uk", DomainGuardService.ExtractRegistrableDomain("https://www.bbc.co.uk/bitesize"));
        Assert.Equal("khanacademy.org", DomainGuardService.ExtractRegistrableDomain("https://ar.khanacademy.org/math"));
    }
}
