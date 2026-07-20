using System.Collections.Concurrent;
using Automation.Browser.Evidence;
using Automation.Core.Artifacts;
using Automation.Core.Configuration;
using Automation.Core.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Automation.Browser;

/// <summary>
/// One isolated browser session for a single test: a fresh context and page, console capture,
/// and failure-evidence collection. Dispose (via <c>await using</c>) after evidence is captured
/// so video/HAR are flushed. Never make product assertions here.
/// </summary>
public sealed class BrowserSession : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    private readonly BrowserOptions _options;
    private readonly IRedactor _redactor;
    private readonly ILogger _logger;
    private readonly string _testDirectory;
    private readonly ConcurrentQueue<ConsoleMessageRecord> _console = new();
    private bool _tracingStarted;
    private bool _evidenceCaptured;

    private BrowserSession(IBrowserContext context, IPage page, BrowserOptions options, IRedactor redactor, string testDirectory, ILogger logger)
    {
        _context = context;
        Page = page;
        _options = options;
        _redactor = redactor;
        _testDirectory = testDirectory;
        _logger = logger;
    }

    /// <summary>The isolated page under test.</summary>
    public IPage Page { get; }

    internal static async Task<BrowserSession> CreateAsync(
        IBrowser browser,
        BrowserOptions options,
        IRedactor redactor,
        string testDirectory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(testDirectory);

        var contextOptions = new BrowserNewContextOptions
        {
            BaseURL = options.BaseUrl,
            IgnoreHTTPSErrors = false,
        };

        if (options.CaptureVideo)
        {
            contextOptions.RecordVideoDir = testDirectory;
        }

        if (options.CaptureHar)
        {
            contextOptions.RecordHarPath = Path.Combine(testDirectory, ArtifactNames.Har);
        }

        IBrowserContext context = await browser.NewContextAsync(contextOptions).ConfigureAwait(false);
        context.SetDefaultNavigationTimeout(options.NavigationTimeoutMs);
        context.SetDefaultTimeout(options.ActionTimeoutMs);

        var session = new BrowserSessionBuilder(context, options, redactor, testDirectory, logger);
        return await session.BuildAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Captures the full failure evidence set (guide section 8.3): screenshot, trace, current
    /// URL, page HTML, and full console. Safe to call once; subsequent calls are ignored.
    /// </summary>
    public async Task CaptureFailureEvidenceAsync()
    {
        if (_evidenceCaptured)
        {
            return;
        }

        _evidenceCaptured = true;

        await TryCaptureAsync("screenshot", async () =>
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(_testDirectory, ArtifactNames.Screenshot),
                FullPage = true,
            }).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (_options.CapturePageHtml)
        {
            await TryCaptureAsync("page-html", async () =>
            {
                string html = await Page.ContentAsync().ConfigureAwait(false);
                await File.WriteAllTextAsync(
                    Path.Combine(_testDirectory, ArtifactNames.PageHtml),
                    BrowserEvidence.RedactHtml(_redactor, html)).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        await TryCaptureAsync("current-url", async () =>
        {
            await File.WriteAllTextAsync(
                Path.Combine(_testDirectory, "current-url.txt"),
                _redactor.RedactUrl(Page.Url)).ConfigureAwait(false);
        }).ConfigureAwait(false);

        await WriteConsoleAsync(errorsOnly: false).ConfigureAwait(false);
        await StopTracingAsync(save: true).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_evidenceCaptured)
        {
            // Passing test: keep console errors as evidence, discard the trace.
            await WriteConsoleAsync(errorsOnly: true).ConfigureAwait(false);
            await StopTracingAsync(save: false).ConfigureAwait(false);
        }

        try
        {
            await _context.CloseAsync().ConfigureAwait(false);
            await _context.DisposeAsync().ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            _logger.LogWarning(ex, "Failed to close browser context cleanly.");
        }
    }

    private async Task StartTracingAsync(CancellationToken cancellationToken)
    {
        await _context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        }).ConfigureAwait(false);
        _tracingStarted = true;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task StopTracingAsync(bool save)
    {
        if (!_tracingStarted)
        {
            return;
        }

        _tracingStarted = false;
        var options = save
            ? new TracingStopOptions { Path = Path.Combine(_testDirectory, ArtifactNames.Trace) }
            : new TracingStopOptions();

        try
        {
            await _context.Tracing.StopAsync(options).ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            _logger.LogWarning(ex, "Failed to stop Playwright tracing.");
        }
    }

    private async Task WriteConsoleAsync(bool errorsOnly)
    {
        IEnumerable<ConsoleMessageRecord> records = errorsOnly
            ? _console.Where(r => r.IsError)
            : _console;

        var lines = records.ToList();
        if (lines.Count == 0)
        {
            return;
        }

        await File.WriteAllTextAsync(
            Path.Combine(_testDirectory, ArtifactNames.BrowserConsole),
            BrowserEvidence.SerializeConsole(_redactor, lines)).ConfigureAwait(false);
    }

    private async Task TryCaptureAsync(string name, Func<Task> capture)
    {
        try
        {
            await capture().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is PlaywrightException or IOException)
        {
            _logger.LogWarning(ex, "Failed to capture {Evidence} evidence.", name);
        }
    }

    private void OnConsoleMessage(IConsoleMessage message) =>
        _console.Enqueue(new ConsoleMessageRecord
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = message.Type,
            Text = message.Text,
            Location = message.Location,
        });

    private void OnPageError(string error) =>
        _console.Enqueue(new ConsoleMessageRecord
        {
            Timestamp = DateTimeOffset.UtcNow,
            Type = "pageerror",
            Text = error,
        });

    /// <summary>Two-phase builder so async tracing setup can complete before the session is handed out.</summary>
    private sealed class BrowserSessionBuilder
    {
        private readonly IBrowserContext _context;
        private readonly BrowserOptions _options;
        private readonly IRedactor _redactor;
        private readonly string _testDirectory;
        private readonly ILogger _logger;

        public BrowserSessionBuilder(IBrowserContext context, BrowserOptions options, IRedactor redactor, string testDirectory, ILogger logger)
        {
            _context = context;
            _options = options;
            _redactor = redactor;
            _testDirectory = testDirectory;
            _logger = logger;
        }

        public async Task<BrowserSession> BuildAsync(CancellationToken cancellationToken)
        {
            IPage page = await _context.NewPageAsync().ConfigureAwait(false);
            var session = new BrowserSession(_context, page, _options, _redactor, _testDirectory, _logger);
            page.Console += (_, message) => session.OnConsoleMessage(message);
            page.PageError += (_, error) => session.OnPageError(error);
            await session.StartTracingAsync(cancellationToken).ConfigureAwait(false);
            return session;
        }
    }
}
