using Automation.Core.Artifacts;
using Automation.Core.Identity;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;

namespace Automation.Core.Logging;

/// <summary>
/// Builds an <see cref="ILoggerFactory"/> backed by NLog with two sinks:
/// a readable console target and a per-test JSON Lines file (guide section 3, Logs).
/// The JSONL file for each test is routed by an ambient scope property so parallel tests
/// never write to one another's files.
/// </summary>
public static class AutomationLogging
{
    public const string TestLogFileProperty = "testLogFile";
    private const string RunLogGdcKey = "automationRunLog";

    /// <summary>Creates a logger factory. Call once per run and dispose at run end.</summary>
    public static ILoggerFactory CreateFactory(RunContext run, string runRootDirectory, bool console = true)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(runRootDirectory);

        var config = new LoggingConfiguration();

        var jsonLayout = new JsonLayout
        {
            IncludeEventProperties = true,
            IncludeScopeProperties = true,
        };
        jsonLayout.Attributes.Add(new JsonAttribute("timestamp", "${date:universalTime=true:format=o}"));
        jsonLayout.Attributes.Add(new JsonAttribute("level", "${level:uppercase=true}"));
        jsonLayout.Attributes.Add(new JsonAttribute("logger", "${logger}"));
        jsonLayout.Attributes.Add(new JsonAttribute("message", "${message}"));
        jsonLayout.Attributes.Add(new JsonAttribute(
            "exception",
            new SimpleLayout("${exception:format=type,message,method,stacktrace:innerFormat=type,message}")));

        // Default file for events emitted outside a per-test scope. The absolute path is passed
        // through a GDC variable so no ':' or '\' appears in the layout *definition* (which would
        // break NLog option parsing); the rendered value may contain them freely.
        string runLog = Path.Combine(runRootDirectory, "run-log.jsonl");
        GlobalDiagnosticsContext.Set(RunLogGdcKey, runLog);
        var fileTarget = new FileTarget("jsonl")
        {
            FileName = new SimpleLayout($"${{scopeproperty:item={TestLogFileProperty}:whenEmpty=${{gdc:item={RunLogGdcKey}}}}}"),
            Layout = jsonLayout,
            KeepFileOpen = false,
            LineEnding = LineEndingMode.LF,
            Encoding = System.Text.Encoding.UTF8,
        };
        config.AddTarget(fileTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);

        if (console)
        {
            var consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = new SimpleLayout(
                    "${date:universalTime=true:format=HH\\:mm\\:ss.fff}|${level:uppercase=true:padding=-5}"
                    + "|${scopeproperty:item=testId:whenEmpty=-}|${logger:shortName=true}|${message}"
                    + "${onexception:${newline}${exception:format=tostring}}"),
            };
            config.AddTarget(consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
        }

        var logFactory = new LogFactory { Configuration = config };
        var providerOptions = new NLogProviderOptions
        {
            CaptureMessageProperties = true,
            IncludeScopes = true,
        };

        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            builder.AddProvider(new NLogLoggerProvider(providerOptions, logFactory));
        });
    }

    /// <summary>
    /// Opens an ambient logging scope for one test. All events logged while the returned
    /// scope is alive carry the test identity and are routed to that test's JSONL file.
    /// </summary>
    public static IDisposable BeginTestScope(ILogger logger, RunContext run, TestIdentity identity, string testDirectory)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(testDirectory);

        string testLog = Path.Combine(testDirectory, ArtifactNames.TestLog);
        var state = new Dictionary<string, object>
        {
            ["runId"] = run.RunId,
            ["testId"] = identity.TestId,
            ["test"] = identity.FullyQualifiedName,
            ["type"] = identity.Type.ToString(),
            ["worker"] = identity.Worker,
            ["browser"] = identity.Browser,
            [TestLogFileProperty] = testLog,
        };

        return logger.BeginScope(state) ?? NullScope.Instance;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
