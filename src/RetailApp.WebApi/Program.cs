using System.Text.Json.Serialization;
using RetailApp.Catalog.Models;
using RetailApp.Catalog.Services;
using RetailApp.Inventory.Services;
using RetailApp.Orders.Models;
using RetailApp.Orders.Services;
using RetailApp.Pricing.Models;
using RetailApp.Pricing.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Register interdependent retail services via Dependency Injection:
// Catalog <- Inventory, Catalog <- Pricing, (Catalog + Inventory + Pricing) <- Orders
builder.Services.AddSingleton<IProductCatalog, ProductCatalogService>();
builder.Services.AddSingleton<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IPricingService, PricingService>();
builder.Services.AddSingleton<IOrderService, OrderService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api");

// Health & Dependency Overview
api.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    application = "RetailApp",
    dependencies = new Dictionary<string, string[]>
    {
        ["RetailApp.Catalog"] = [],
        ["RetailApp.Inventory"] = ["RetailApp.Catalog"],
        ["RetailApp.Pricing"] = ["RetailApp.Catalog"],
        ["RetailApp.Orders"] = ["RetailApp.Catalog", "RetailApp.Inventory", "RetailApp.Pricing"],
        ["RetailApp.WebApi"] = ["RetailApp.Catalog", "RetailApp.Inventory", "RetailApp.Pricing", "RetailApp.Orders"]
    }
}));

// Catalog + Inventory combined storefront listing
api.MapGet("/products", (
    IProductCatalog catalog,
    IInventoryService inventory,
    string? category,
    string? search) =>
{
    var products = catalog.GetAll(category, search)
        .Select(p =>
        {
            var stock = inventory.GetStock(p.Id);
            return new
            {
                p.Id,
                p.Name,
                p.Category,
                p.Description,
                p.UnitPrice,
                QuantityAvailable = stock?.QuantityAvailable ?? 0,
                IsLowStock = stock?.IsLowStock ?? true
            };
        });

    return Results.Ok(products);
});

api.MapPost("/products", (
    AddProductApiRequest request,
    IProductCatalog catalog,
    IInventoryService inventory) =>
{
    try
    {
        var created = catalog.AddProduct(new CreateProductRequest(
            request.Id,
            request.Name,
            request.Category,
            request.Description,
            request.UnitPrice
        ));

        var initialStock = 0;
        if (request.InitialStock > 0)
        {
            var stock = inventory.Restock(created.Id, request.InitialStock);
            initialStock = stock.QuantityAvailable;
        }

        return Results.Created($"/api/products/{created.Id}", new
        {
            created.Id,
            created.Name,
            created.Category,
            created.Description,
            created.UnitPrice,
            QuantityAvailable = initialStock
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Inventory endpoints
api.MapGet("/inventory", (IInventoryService inventory) =>
    Results.Ok(inventory.GetAllStock()));

api.MapPost("/inventory/{productId}/restock", (
    string productId,
    RestockRequest request,
    IInventoryService inventory) =>
{
    try
    {
        var updated = inventory.Restock(productId, request.Quantity);
        return Results.Ok(updated);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Pricing quote endpoint
api.MapPost("/pricing/quote", (QuoteApiRequest request, IPricingService pricing) =>
{
    try
    {
        var quote = pricing.CalculateQuote(request.Items, request.PromoCode);
        return Results.Ok(quote);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Order endpoints
api.MapGet("/orders", (IOrderService orders) =>
    Results.Ok(orders.GetAllOrders()));

api.MapGet("/orders/{orderId}", (string orderId, IOrderService orders) =>
{
    var order = orders.GetById(orderId);
    return order is not null
        ? Results.Ok(order)
        : Results.NotFound(new { error = $"Order '{orderId}' was not found." });
});

api.MapPost("/orders", (PlaceOrderRequest request, IOrderService orders) =>
{
    var result = orders.PlaceOrder(request);
    return result.Success
        ? Results.Created($"/api/orders/{result.Order!.OrderId}", result.Order)
        : Results.BadRequest(new { error = result.Error });
});

api.MapPost("/orders/{orderId}/cancel", (string orderId, IOrderService orders) =>
{
    var result = orders.CancelOrder(orderId);
    if (result.Success)
        return Results.Ok(result.Order);

    return result.Order is null
        ? Results.NotFound(new { error = result.Error })
        : Results.Conflict(new { error = result.Error });
});

app.Run();

public sealed record AddProductApiRequest(
    string Id,
    string Name,
    string Category,
    string Description,
    decimal UnitPrice,
    int InitialStock = 0
);

public sealed record RestockRequest(int Quantity);

public sealed record QuoteApiRequest(
    IReadOnlyList<PricingLineRequest> Items,
    string? PromoCode = null
);
