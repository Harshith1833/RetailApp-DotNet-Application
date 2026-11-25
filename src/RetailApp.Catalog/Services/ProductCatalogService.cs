using System.Collections.Concurrent;
using RetailApp.Catalog.Models;

namespace RetailApp.Catalog.Services;

public interface IProductCatalog
{
    IReadOnlyList<Product> GetAll(string? category = null, string? search = null);
    Product? GetById(string productId);
    Product AddProduct(CreateProductRequest request);
}

public sealed class ProductCatalogService : IProductCatalog
{
    private readonly ConcurrentDictionary<string, Product> _products = new(StringComparer.OrdinalIgnoreCase);

    public ProductCatalogService()
    {
        SeedDefaultProducts();
    }

    public IReadOnlyList<Product> GetAll(string? category = null, string? search = null)
    {
        IEnumerable<Product> query = _products.Values;

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.Ordinal) ||
                p.Description.Contains(term, StringComparison.Ordinal) ||
                p.Id.Contains(term, StringComparison.Ordinal));
        }

        return query.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Product? GetById(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        return _products.TryGetValue(productId.Trim(), out var product) ? product : null;
    }

    public Product AddProduct(CreateProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = request.Id?.Trim().ToUpperInvariant()
            ?? throw new ArgumentException("Product ID is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Product ID is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Product name is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Category))
            throw new ArgumentException("Product category is required.", nameof(request));
        if (request.UnitPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Unit price must be greater than zero.");

        var product = new Product(
            Id: id,
            Name: request.Name.Trim(),
            Category: request.Category.Trim(),
            Description: request.Description?.Trim() ?? string.Empty,
            UnitPrice: decimal.Round(request.UnitPrice, 2)
        );

        if (!_products.TryAdd(product.Id, product))
            throw new InvalidOperationException($"Product '{product.Id}' already exists.");

        return product;
    }

    private void SeedDefaultProducts()
    {
        var defaults = new[]
        {
            new Product("SKU-1001", "Wireless Noise-Canceling Headphones", "Electronics", "Over-ear Bluetooth headphones with 30-hour battery life.", 149.99m),
            new Product("SKU-1002", "Mechanical Compact Keyboard", "Electronics", "Tactile hot-swappable 75% keyboard with backlight.", 89.50m),
            new Product("SKU-2001", "Everyday Running Sneakers", "Footwear", "Breathable knit sneakers built for daily comfort.", 74.00m),
            new Product("SKU-2002", "Classic Cotton Hoodie", "Apparel", "Mid-weight brushed fleece pullover hoodie.", 48.00m),
            new Product("SKU-3001", "Insulated Stainless Tumbler", "Home", "Double-wall 24oz tumbler keeps drinks cold for 24 hours.", 24.99m),
            new Product("SKU-3002", "Smart LED Desk Lamp", "Home", "Dimmable desk lamp with adjustable warm-to-cool color temperature.", 39.95m)
        };

        foreach (var product in defaults)
        {
            _products[product.Id] = product;
        }
    }
}
