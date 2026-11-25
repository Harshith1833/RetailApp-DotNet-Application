namespace RetailApp.Catalog.Models;

public sealed record Product(
    string Id,
    string Name,
    string Category,
    string Description,
    decimal UnitPrice
);

public sealed record CreateProductRequest(
    string Id,
    string Name,
    string Category,
    string Description,
    decimal UnitPrice
);
