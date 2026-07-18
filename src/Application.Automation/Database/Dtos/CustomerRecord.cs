namespace Application.Automation.Database.Dtos;

/// <summary>Sample row shape mapped from the reviewed customer query.</summary>
public sealed record CustomerRecord
{
    public required string Id { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string Status { get; init; }
}
