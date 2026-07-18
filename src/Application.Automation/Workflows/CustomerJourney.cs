using Application.Automation.Api;
using Application.Automation.Api.Dtos;
using Application.Automation.Database;
using Application.Automation.Database.Dtos;
using Automation.SqlServer;

namespace Application.Automation.Workflows;

/// <summary>
/// Sample cross-channel workflow combining API and read-only SQL. Workflows compose framework
/// capabilities into business operations; they still contain no NUnit assertions (guide section 5.1).
/// </summary>
public sealed class CustomerJourney
{
    private readonly OrdersApiClient _orders;
    private readonly IReadOnlySqlClient _sql;

    public CustomerJourney(OrdersApiClient orders, IReadOnlySqlClient sql)
    {
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        _sql = sql ?? throw new ArgumentNullException(nameof(sql));
    }

    /// <summary>Reads a customer via SQL and returns their orders via the API.</summary>
    public async Task<CustomerJourneyResult> LoadCustomerWithOrdersAsync(string customerId, CancellationToken cancellationToken = default)
    {
        CustomerRecord? customer = await _sql.QuerySingleOrDefaultAsync<CustomerRecord>(
            CustomerQueries.GetById(customerId),
            cancellationToken);

        IReadOnlyList<OrderDto> orders = customer is null
            ? []
            : (await _orders.ListOrdersAsync(customerId, cancellationToken)).Data ?? [];

        return new CustomerJourneyResult(customer, orders);
    }
}

public sealed record CustomerJourneyResult(CustomerRecord? Customer, IReadOnlyList<OrderDto> Orders);
