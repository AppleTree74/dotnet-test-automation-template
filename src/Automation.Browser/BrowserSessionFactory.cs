using Automation.Core.Configuration;
using Automation.Core.Redaction;
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
    private readonly IRedactor _redactor;
    private readonly ILogger<BrowserSession> _logger;

    public BrowserSessionFactory(PlaywrightDriver driver, BrowserOptions options, IRedactor redactor, ILogger<BrowserSession> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _redactor = redactor ?? throw new ArgumentNullException(nameof(redactor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BrowserSession> CreateAsync(BrowserKind kind, string testDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testDirectory);

        Microsoft.Playwright.IBrowser browser = await _driver.GetBrowserAsync(kind, cancellationToken).ConfigureAwait(false);
        return await BrowserSession.CreateAsync(browser, _options, _redactor, testDirectory, _logger, cancellationToken).ConfigureAwait(false);
    }
}
