namespace RetailApp.Inventory.Models;

public sealed record StockItem(
    string ProductId,
    string ProductName,
    int QuantityAvailable,
    bool IsLowStock
);

public sealed record StockReservationResult(
    bool Success,
    string Message
);
