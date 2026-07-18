using Application.Automation.Api.Dtos;
using Automation.Api;

namespace Application.Automation.Api;

/// <summary>
/// Sample typed product client built on the framework <see cref="IApiClient"/>. It owns product
/// endpoints and DTOs and returns responses for the test to assert; it never asserts itself.
/// </summary>
public sealed class OrdersApiClient
{
    private readonly IApiClient _apiClient;

    public OrdersApiClient(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<OrderDto>> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/orders/{Uri.EscapeDataString(orderId)}", UriKind.Relative));
        return await _apiClient.SendAsync<OrderDto>(request, cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<OrderDto>>> ListOrdersAsync(string customerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerId);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"/orders?customerId={Uri.EscapeDataString(customerId)}", UriKind.Relative));
        return await _apiClient.SendAsync<IReadOnlyList<OrderDto>>(request, cancellationToken);
    }
}
