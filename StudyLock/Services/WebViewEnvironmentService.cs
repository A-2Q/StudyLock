using System.IO;
using Microsoft.Web.WebView2.Core;
using StudyLock.Models;

namespace StudyLock.Services;

public sealed class WebViewEnvironmentService
{
    private readonly SessionStateService _stateService;
    private CoreWebView2Environment? _studyEnvironment;
    private CoreWebView2Environment? _childEnvironment;

    public WebViewEnvironmentService(SessionStateService stateService)
    {
        _stateService = stateService;
    }

    public async Task<CoreWebView2Environment> GetEnvironmentAsync(SessionMode mode)
    {
        if (mode == SessionMode.Study)
        {
            if (_studyEnvironment is null)
            {
                Directory.CreateDirectory(_stateService.StudyWebViewProfilePath);
                _studyEnvironment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: _stateService.StudyWebViewProfilePath);
            }
            return _studyEnvironment;
        }
        else
        {
            if (_childEnvironment is null)
            {
                Directory.CreateDirectory(_stateService.ChildWebViewProfilePath);
                _childEnvironment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: _stateService.ChildWebViewProfilePath);
            }
            return _childEnvironment;
        }
    }
}
