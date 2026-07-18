using Automation.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Automation.Browser;

/// <summary>Creates one isolated <see cref="BrowserSession"/> per test.</summary>
public interface IBrowserSessionFactory
{
    Task<BrowserSession> CreateAsync(BrowserKind kind, string testDirectory, CancellationToken cancellationToken = default);
}

public sealed class BrowserSessionFactory : IBrowserSessionFactory
{
    private readonly PlaywrightDriver _driver;
    private readonly BrowserOptions _options;
    private readonly ILogger<BrowserSession> _logger;

    public BrowserSessionFactory(PlaywrightDriver driver, BrowserOptions options, ILogger<BrowserSession> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BrowserSession> CreateAsync(BrowserKind kind, string testDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testDirectory);

        Microsoft.Playwright.IBrowser browser = await _driver.GetBrowserAsync(kind, cancellationToken).ConfigureAwait(false);
        return await BrowserSession.CreateAsync(browser, _options, testDirectory, _logger, cancellationToken).ConfigureAwait(false);
    }
}
