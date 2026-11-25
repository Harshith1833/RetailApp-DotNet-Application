# RetailApp (.NET 10 Modular Retailer Application)

A simple, working multi-project .NET 10 retail application demonstrating clean modular architecture and inter-project dependencies.

## Project Dependency Graph

```text
RetailApp.Catalog           (Base class library: products & catalog lookup)
   ▲           ▲
   │           │
RetailApp.Inventory   RetailApp.Pricing   (Both depend on Catalog)
   ▲           ▲           ▲
   └─────┬─────┘           │
         │                 │
   RetailApp.Orders ───────┘              (Depends on Catalog, Inventory, Pricing)
         ▲
         │
   RetailApp.WebApi                       (Depends on all 4 libraries; hosts REST API + Storefront UI)
```

### What each project owns

| Project | Dependencies | Responsibility |
| --- | --- | --- |
| `RetailApp.Catalog` | *(none)* | Product models and thread-safe in-memory product catalog with seeded retail items. |
| `RetailApp.Inventory` | `Catalog` | Stock tracking, low-stock flags, reserving stock, releasing stock, and restocking (validated against `IProductCatalog`). |
| `RetailApp.Pricing` | `Catalog` | Price quotes using catalog prices, 5% bulk discount (5+ total items), promo codes (`SAVE10`, `VIP20`, `WELCOME5`), and 8% sales tax. |
| `RetailApp.Orders` | `Catalog`, `Inventory`, `Pricing` | Places orders by validating catalog items, atomically reserving inventory (with rollback on insufficient stock), calculating quotes, and restoring inventory on cancellation. |
| `RetailApp.WebApi` | All 4 libraries | Wires services via .NET Dependency Injection, exposes REST API endpoints, and serves an interactive Storefront UI at `/`. |

## Running the Application

```powershell
dotnet build RetailApp.sln
dotnet run --project src/RetailApp.WebApi/RetailApp.WebApi.csproj
```

Then open the URL printed in the console (for example `http://localhost:5000` or `http://localhost:52xx`) to use the Storefront UI, or call the REST API directly.

## REST API Endpoints

- `GET /api/health` — Application health and project dependency map
- `GET /api/products?category=&search=` — List catalog products with live inventory counts
- `POST /api/products` — Add a new product (with optional `initialStock`)
- `GET /api/inventory` — List stock levels for all products
- `POST /api/inventory/{productId}/restock` — Add inventory (`{ "quantity": 10 }`)
- `POST /api/pricing/quote` — Calculate a cart quote with discounts and tax
- `GET /api/orders` — List all orders
- `GET /api/orders/{orderId}` — Get an order by ID
- `POST /api/orders` — Place an order (reserves stock automatically)
- `POST /api/orders/{orderId}/cancel` — Cancel an order and return reserved stock to inventory


## Explaination on project
RetailApp is a lightweight .NET 10 modular e-commerce / retail store application built as a 5-project solution where each business domain lives in its own library and depends on the others through .NET project references and Dependency Injection.

The 5 Modules & How They Depend on Each Other
RetailApp.Catalog (no dependencies)
The product master. Stores product details (SKU, name, category, description, unit price) and comes pre-seeded with 6 retail products (Electronics, Footwear, Apparel, Home).
RetailApp.Inventory (depends on Catalog)
Tracks available stock per product, flags low stock (≤ 5 units), and handles reserving, releasing, and restocking inventory — always validating SKUs against Catalog.
RetailApp.Pricing (depends on Catalog)
Calculates cart totals using official prices from Catalog, automatically applying a 5% bulk discount (5+ items), promo codes (SAVE10, VIP20, WELCOME5), and 8% sales tax.
RetailApp.Orders (depends on Catalog, Inventory, and Pricing)
Orchestrates the checkout lifecycle:
Validates items against Catalog
Reserves stock via Inventory (and rolls back if any item is out of stock)
Calculates the final receipt via Pricing
Restores stock back to Inventory if an order is cancelled
RetailApp.WebApi (depends on all 4 libraries)
The entry point. Wires all services together via built-in .NET Dependency Injection, exposes REST API endpoints (/api/products, /api/inventory, /api/pricing/quote, /api/orders), and serves a ready-to-use Storefront Web UI at /.
In One Sentence

It simulates a real retail backend (Catalog → Inventory & Pricing → Orders → Web API/UI) in a clean, self-contained .NET 10 solution with clear service boundaries and zero external database or package setup required.

## Clear the old bin/obj folders before rebuilding (they store cached paths from the previous location):
powershell
cd "C:\Users\HA40183347\OneDrive - Wipro\TJX\POC-Application-Github"
Get-ChildItem -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
dotnet build RetailApp.sln
dotnet run --project src/RetailApp.WebApi/RetailApp.WebApi.csproj