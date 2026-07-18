namespace Application.Automation.Api.Dtos;

/// <summary>Sample product DTO. Product endpoints and DTOs live in Application.Automation (guide section 9.2).</summary>
public sealed record OrderDto
{
    public required string Id { get; init; }

    public required string Status { get; init; }

    public decimal Total { get; init; }

    public string? CustomerId { get; init; }
}
