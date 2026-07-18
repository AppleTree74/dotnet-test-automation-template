using Automation.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Automation.Browser;

/// <summary>
/// Owns the Playwright process and browser instances. A browser process MAY be reused across
/// tests, but every UI test receives a fresh <see cref="IBrowserContext"/> and page
/// (guide section 8.1). Register as a singleton and dispose at run end.
/// </summary>
public sealed class PlaywrightDriver : IAsyncDisposable
{
    private readonly BrowserOptions _options;
    private readonly ILogger<PlaywrightDriver> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<BrowserKind, IBrowser> _browsers = new();
    private IPlaywright? _playwright;

    public PlaywrightDriver(BrowserOptions options, ILogger<PlaywrightDriver> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Returns a shared browser instance for the requested kind, launching it on first use.</summary>
    public async Task<IBrowser> GetBrowserAsync(BrowserKind kind, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);

            if (_browsers.TryGetValue(kind, out IBrowser? existing) && existing.IsConnected)
            {
                return existing;
            }

            IBrowserType browserType = kind switch
            {
                BrowserKind.Chromium => _playwright.Chromium,
                BrowserKind.Firefox => _playwright.Firefox,
                BrowserKind.Webkit => _playwright.Webkit,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported browser."),
            };

            _logger.LogInformation("Launching {Browser} (headless={Headless}).", kind, _options.Headless);
            IBrowser browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _options.Headless,
                SlowMo = _options.SlowMoMs,
            }).ConfigureAwait(false);

            _browsers[kind] = browser;
            return browser;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (IBrowser browser in _browsers.Values)
        {
            try
            {
                await browser.DisposeAsync().ConfigureAwait(false);
            }
            catch (PlaywrightException ex)
            {
                _logger.LogWarning(ex, "Failed to dispose a browser cleanly.");
            }
        }

        _browsers.Clear();
        _playwright?.Dispose();
        _playwright = null;
        _gate.Dispose();
    }
}
