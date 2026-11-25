using RetailApp.Catalog.Services;
using RetailApp.Inventory.Models;

namespace RetailApp.Inventory.Services;

public interface IInventoryService
{
    IReadOnlyList<StockItem> GetAllStock();
    StockItem? GetStock(string productId);
    StockReservationResult Reserve(string productId, int quantity);
    void Release(string productId, int quantity);
    StockItem Restock(string productId, int quantity);
}

public sealed class InventoryService : IInventoryService
{
    private const int LowStockThreshold = 5;

    private readonly IProductCatalog _catalog;
    private readonly Lock _syncLock = new();
    private readonly Dictionary<string, int> _stockLevels = new(StringComparer.OrdinalIgnoreCase);

    public InventoryService(IProductCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        SeedInitialStock();
    }

    public IReadOnlyList<StockItem> GetAllStock()
    {
        var products = _catalog.GetAll();
        lock (_syncLock)
        {
            return products
                .Select(p =>
                {
                    var qty = _stockLevels.GetValueOrDefault(p.Id, 0);
                    return new StockItem(p.Id, p.Name, qty, qty <= LowStockThreshold);
                })
                .ToList();
        }
    }

    public StockItem? GetStock(string productId)
    {
        var product = _catalog.GetById(productId);
        if (product is null)
            return null;

        lock (_syncLock)
        {
            var qty = _stockLevels.GetValueOrDefault(product.Id, 0);
            return new StockItem(product.Id, product.Name, qty, qty <= LowStockThreshold);
        }
    }

    public StockReservationResult Reserve(string productId, int quantity)
    {
        if (quantity <= 0)
            return new StockReservationResult(false, "Reservation quantity must be greater than zero.");

        var product = _catalog.GetById(productId);
        if (product is null)
            return new StockReservationResult(false, $"Product '{productId}' does not exist in catalog.");

        lock (_syncLock)
        {
            var available = _stockLevels.GetValueOrDefault(product.Id, 0);
            if (available < quantity)
            {
                return new StockReservationResult(
                    false,
                    $"Insufficient stock for '{product.Name}' ({product.Id}). Requested: {quantity}, available: {available}."
                );
            }

            _stockLevels[product.Id] = available - quantity;
            return new StockReservationResult(true, $"Reserved {quantity} unit(s) of {product.Id}.");
        }
    }

    public void Release(string productId, int quantity)
    {
        if (quantity <= 0)
            return;

        var product = _catalog.GetById(productId);
        if (product is null)
            return;

        lock (_syncLock)
        {
            var current = _stockLevels.GetValueOrDefault(product.Id, 0);
            _stockLevels[product.Id] = current + quantity;
        }
    }

    public StockItem Restock(string productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Restock quantity must be greater than zero.");

        var product = _catalog.GetById(productId)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found in catalog.");

        lock (_syncLock)
        {
            var updated = quantity;
            _stockLevels[product.Id] = updated;
            return new StockItem(product.Id, product.Name, updated, updated <= LowStockThreshold);
        }
    }

    private void SeedInitialStock()
    {
        var initialStock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["SKU-1001"] = 12,
            ["SKU-1002"] = 8,
            ["SKU-2001"] = 15,
            ["SKU-2002"] = 20,
            ["SKU-3001"] = 30,
            ["SKU-3002"] = 4
        };

        foreach (var (sku, qty) in initialStock)
        {
            if (_catalog.GetById(sku) is not null)
            {
                _stockLevels[sku] = qty;
            }
        }
    }
}
