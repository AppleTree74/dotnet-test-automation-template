namespace Automation.Browser.Evidence;

/// <summary>One captured browser console entry, serialized to <c>browser-console.jsonl</c>.</summary>
public sealed record ConsoleMessageRecord
{
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Console severity: <c>error</c>, <c>warning</c>, <c>log</c>, <c>info</c>, <c>debug</c>, or <c>pageerror</c>.</summary>
    public required string Type { get; init; }

    public required string Text { get; init; }

    public string? Location { get; init; }

    public bool IsError =>
        Type.Equals("error", StringComparison.OrdinalIgnoreCase)
        || Type.Equals("pageerror", StringComparison.OrdinalIgnoreCase);
}
