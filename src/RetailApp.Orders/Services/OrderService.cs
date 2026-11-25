using RetailApp.Catalog.Services;
using RetailApp.Inventory.Services;
using RetailApp.Orders.Models;
using RetailApp.Pricing.Models;
using RetailApp.Pricing.Services;

namespace RetailApp.Orders.Services;

public interface IOrderService
{
    OrderResult PlaceOrder(PlaceOrderRequest request);
    OrderResult CancelOrder(string orderId);
    IReadOnlyList<Order> GetAllOrders();
    Order? GetById(string orderId);
}

public sealed class OrderService : IOrderService
{
    private readonly IProductCatalog _catalog;
    private readonly IInventoryService _inventory;
    private readonly IPricingService _pricing;
    private readonly Lock _syncLock = new();
    private readonly Dictionary<string, Order> _orders = new(StringComparer.OrdinalIgnoreCase);
    private int _sequence = 1000;

    public OrderService(
        IProductCatalog catalog,
        IInventoryService inventory,
        IPricingService pricing)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
    }

    public OrderResult PlaceOrder(PlaceOrderRequest request)
    {
        if (request is null)
            return new OrderResult(false, "Order request is required.", null);

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return new OrderResult(false, "Customer name is required.", null);

        if (request.Items is null || request.Items.Count == 0)
            return new OrderResult(false, "Order must contain at least one item.", null);

        // Consolidate duplicate product IDs into single lines
        var normalizedLines = request.Items
            .GroupBy(i => i.ProductId?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PricingLineRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        foreach (var line in normalizedLines)
        {
            if (line.Quantity <= 0)
                return new OrderResult(false, $"Quantity for '{line.ProductId}' must be greater than zero.", null);

            if (_catalog.GetById(line.ProductId) is null)
                return new OrderResult(false, $"Product '{line.ProductId}' was not found in catalog.", null);
        }

        // Atomically reserve stock for every line, rolling back on failure
        var reserved = new List<PricingLineRequest>();
        foreach (var line in normalizedLines)
        {
            var reservation = _inventory.Reserve(line.ProductId, line.Quantity);
            if (!reservation.Success)
            {
                foreach (var previous in reserved)
                {
                    _inventory.Release(previous.ProductId, previous.Quantity);
                }

                return new OrderResult(false, reservation.Message, null);
            }

            reserved.Add(line);
        }

        PriceQuote quote;
        try
        {
            quote = _pricing.CalculateQuote(normalizedLines, request.PromoCode);
        }
        catch (Exception ex)
        {
            foreach (var item in reserved)
            {
                _inventory.Release(item.ProductId, item.Quantity);
            }

            return new OrderResult(false, ex.Message, null);
        }

        lock (_syncLock)
        {
            var orderId = $"ORD-{Interlocked.Increment(ref _sequence)}";
            var order = new Order(
                OrderId: orderId,
                CustomerName: request.CustomerName.Trim(),
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Status: OrderStatus.Confirmed,
                Quote: quote
            );

            _orders[orderId] = order;
            return new OrderResult(true, null, order);
        }
    }

    public OrderResult CancelOrder(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return new OrderResult(false, "Order ID is required.", null);

        Order cancelledOrder;
        lock (_syncLock)
        {
            if (!_orders.TryGetValue(orderId.Trim(), out var existing))
                return new OrderResult(false, $"Order '{orderId}' was not found.", null);

            if (existing.Status == OrderStatus.Cancelled)
                return new OrderResult(false, $"Order '{existing.OrderId}' is already cancelled.", existing);

            cancelledOrder = existing with
            {
                Status = OrderStatus.Cancelled,
                CancelledAtUtc = DateTimeOffset.UtcNow
            };

            _orders[existing.OrderId] = cancelledOrder;
        }

        // Return reserved stock back to inventory
        foreach (var item in cancelledOrder.Quote.Items)
        {
            _inventory.Release(item.ProductId, item.Quantity);
        }

        return new OrderResult(true, null, cancelledOrder);
    }

    public IReadOnlyList<Order> GetAllOrders()
    {
        lock (_syncLock)
        {
            return _orders.Values
                .OrderByDescending(o => o.CreatedAtUtc)
                .ToList();
        }
    }

    public Order? GetById(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return null;

        lock (_syncLock)
        {
            return _orders.TryGetValue(orderId.Trim(), out var order) ? order : null;
        }
    }
}
