using RetailApp.Pricing.Models;

namespace RetailApp.Orders.Models;

public enum OrderStatus
{
    Confirmed,
    Cancelled
}

public sealed record OrderItemRequest(
    string ProductId,
    int Quantity
);

public sealed record PlaceOrderRequest(
    string CustomerName,
    IReadOnlyList<OrderItemRequest> Items,
    string? PromoCode = null
);

public sealed record Order(
    string OrderId,
    string CustomerName,
    DateTimeOffset CreatedAtUtc,
    OrderStatus Status,
    PriceQuote Quote,
    DateTimeOffset? CancelledAtUtc = null
);

public sealed record OrderResult(
    bool Success,
    string? Error,
    Order? Order
);
