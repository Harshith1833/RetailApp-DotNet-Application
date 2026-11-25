using RetailApp.Catalog.Services;
using RetailApp.Pricing.Models;

namespace RetailApp.Pricing.Services;

public interface IPricingService
{
    PriceQuote CalculateQuote(IReadOnlyList<PricingLineRequest> items, string? promoCode = null);
}

public sealed class PricingService : IPricingService
{
    private const decimal DefaultTaxRate = 0.08m;
    private const int BulkDiscountMinQuantity = 5;
    private const decimal BulkDiscountRate = 0.05m;

    private readonly IProductCatalog _catalog;

    public PricingService(IProductCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public PriceQuote CalculateQuote(IReadOnlyList<PricingLineRequest> items, string? promoCode = null)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("At least one item is required to calculate a price quote.", nameof(items));

        var pricedLines = new List<PricedLineItem>(items.Count);

        foreach (var line in items)
        {
            if (line.Quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(items), $"Quantity for '{line.ProductId}' must be greater than zero.");

            var product = _catalog.GetById(line.ProductId)
                ?? throw new KeyNotFoundException($"Product '{line.ProductId}' was not found in catalog.");

            var lineSubtotal = decimal.Round(product.UnitPrice * line.Quantity, 2);
            pricedLines.Add(new PricedLineItem(
                ProductId: product.Id,
                ProductName: product.Name,
                UnitPrice: product.UnitPrice,
                Quantity: line.Quantity,
                LineSubtotal: lineSubtotal
            ));
        }

        var subtotal = pricedLines.Sum(l => l.LineSubtotal);
        var totalQuantity = pricedLines.Sum(l => l.Quantity);

        var bulkDiscount = totalQuantity > BulkDiscountMinQuantity
            ? decimal.Round(subtotal * BulkDiscountRate, 2)
            : 0m;

        var afterBulk = subtotal - bulkDiscount;
        var (appliedPromo, promoDiscount) = ApplyPromoCode(afterBulk, promoCode);

        var taxableAmount = Math.Max(0m, afterBulk + promoDiscount);
        var taxAmount = decimal.Round(taxableAmount * DefaultTaxRate, 2);
        var grandTotal = decimal.Round(taxableAmount + taxAmount, 2);

        return new PriceQuote(
            Items: pricedLines,
            Subtotal: subtotal,
            BulkDiscount: bulkDiscount,
            AppliedPromoCode: appliedPromo,
            PromoDiscount: promoDiscount,
            TaxRate: DefaultTaxRate,
            TaxAmount: taxAmount,
            GrandTotal: grandTotal
        );
    }

    private static (string? AppliedCode, decimal Discount) ApplyPromoCode(decimal eligibleAmount, string? promoCode)
    {
        if (string.IsNullOrWhiteSpace(promoCode))
            return (null, 0m);

        var normalized = promoCode.Trim().ToUpperInvariant();
        return normalized switch
        {
            "SAVE10" => ("SAVE10", decimal.Round(eligibleAmount * 0.10m, 2)),
            "VIP20" => ("VIP20", decimal.Round(eligibleAmount * 0.20m, 2)),
            "WELCOME5" when eligibleAmount >= 25m => ("WELCOME5", 5.00m),
            _ => (null, 0m)
        };
    }
}
