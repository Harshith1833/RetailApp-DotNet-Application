namespace RetailApp.Pricing.Models;

public sealed record PricingLineRequest(
    string ProductId,
    int Quantity
);

public sealed record PricedLineItem(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineSubtotal
);

public sealed record PriceQuote(
    IReadOnlyList<PricedLineItem> Items,
    decimal Subtotal,
    decimal BulkDiscount,
    string? AppliedPromoCode,
    decimal PromoDiscount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal GrandTotal
);
