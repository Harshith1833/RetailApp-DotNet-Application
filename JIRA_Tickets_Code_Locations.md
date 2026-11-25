# RetailApp — Jira Tickets to Code Locations Mapping

This document maps all 10 Jira bug tickets (`RETAIL-1` through `RETAIL-10`) to the exact files and line numbers in the RetailApp .NET application where each defect exists.

| Ticket ID | Area / Category | Summary | File Path | Line Number(s) |
| --- | --- | --- | --- | --- |
| **RETAIL-1** | Navigation Bar | Clicking "Orders" in the top navigation bar navigates to Product Catalog instead of Recent Orders | `src/RetailApp.WebApi/wwwroot/index.html` | Line **208** |
| **RETAIL-2** | Login | Valid user credentials always fail sign-in with "Invalid email or password" | `src/RetailApp.WebApi/Program.cs` | Lines **169–172** |
| **RETAIL-3** | Buttons (Shopping Cart) | Clicking the `-` quantity button in Shopping Cart increases item quantity instead of decreasing it | `src/RetailApp.WebApi/wwwroot/index.html` | Line **505** |
| **RETAIL-4** | Buttons (Catalog Filters) | Clicking "Reset Filters" does not clear the selected category or refresh the product list | `src/RetailApp.WebApi/wwwroot/index.html` | Lines **404–406** (button at Line **234**) |
| **RETAIL-5** | Internal Server Error (HTTP 500) | Clicking "View Receipt" on a confirmed order throws HTTP 500 Internal Server Error | `src/RetailApp.WebApi/Program.cs` | Line **203** (endpoint Lines **197–219**) |
| **RETAIL-6** | Buttons / Inventory Restock | Clicking "+5 Stock" overwrites available stock to 5 instead of adding 5 units | `src/RetailApp.Inventory/Services/InventoryService.cs` | Lines **108–109** |
| **RETAIL-7** | Catalog Search | Product search is case-sensitive and returns no results for lowercase search terms | `src/RetailApp.Catalog/Services/ProductCatalogService.cs` | Lines **35–37** |
| **RETAIL-8** | Pricing / Promo Codes | Applying a promo code increases the order total instead of subtracting the discount | `src/RetailApp.Pricing/Services/PricingService.cs` | Line **59** |
| **RETAIL-9** | Pricing / Bulk Discount | Ordering exactly 5 items does not apply the advertised 5% bulk discount | `src/RetailApp.Pricing/Services/PricingService.cs` | Line **52** |
| **RETAIL-10** | Orders / Cancellation | Cancelling a confirmed order does not restore reserved stock back to inventory | `src/RetailApp.Orders/Services/OrderService.cs` | Line **137** (lines **135–138**) |

---

## Detailed Code Breakdown

### 1. RETAIL-1 — Navigation Bar "Orders" Link Redirects to Catalog
- **File:** `src/RetailApp.WebApi/wwwroot/index.html`
- **Line:** `208`
- **Faulty Code:**
  ```html
  <a id="navOrders" href="#catalogSection" class="nav-link" onclick="navigateTo(event, 'catalog')">Orders</a>
  ```
- **Cause:** Both `href` and the `navigateTo` argument point to `catalog` instead of `orders` (`#ordersSection` / `'orders'`).

### 2. RETAIL-2 — Valid Credentials Fail Authentication on Login
- **File:** `src/RetailApp.WebApi/Program.cs`
- **Lines:** `169–172`
- **Faulty Code:**
  ```csharp
  var normalizedPassword = request.Password.Trim().ToLowerInvariant();

  if (!demoUsers.TryGetValue(normalizedEmail, out var account) ||
      !string.Equals(normalizedPassword, account.Password, StringComparison.Ordinal))
  ```
- **Cause:** The submitted password is converted to lowercase (`ToLowerInvariant()`) before an ordinal case-sensitive comparison against stored mixed-case passwords (`Shop@2026`, `Admin@2026`), so valid logins always fail with `401 Unauthorized`.

### 3. RETAIL-3 — Cart Minus (`-`) Button Increments Quantity
- **File:** `src/RetailApp.WebApi/wwwroot/index.html`
- **Line:** `505`
- **Faulty Code:**
  ```html
  <button class="secondary" onclick="updateQty('${item.productId}', 1)">-</button>
  ```
- **Cause:** The minus button passes delta `1` instead of `-1` into `updateQty(...)`.

### 4. RETAIL-4 — "Reset Filters" Button Fails to Clear Category and Reload Products
- **File:** `src/RetailApp.WebApi/wwwroot/index.html`
- **Lines:** `404–406` (bound to button at line `234`)
- **Faulty Code:**
  ```javascript
  function resetFilters() {
    document.getElementById('searchInput').value = '';
  }
  ```
- **Cause:** `resetFilters()` clears `#searchInput` only, without resetting `#categoryFilter.value = ''` and without invoking `loadProducts()`.

### 5. RETAIL-5 — "View Receipt" on Confirmed Order Throws HTTP 500 Internal Server Error
- **File:** `src/RetailApp.WebApi/Program.cs`
- **Line:** `203` (inside `GET /api/orders/{orderId}/receipt`, lines `197–219`)
- **Faulty Code:**
  ```csharp
  var cancelledSummary = order.CancelledAtUtc!.Value.ToString("u");
  ```
- **Cause:** Accessing `.Value` directly on nullable `order.CancelledAtUtc` when the order is `Confirmed` (`CancelledAtUtc == null`) throws an unhandled `InvalidOperationException`, returning HTTP `500 Internal Server Error`.

### 6. RETAIL-6 — "+5 Stock" Overwrites Stock Quantity to 5 Instead of Incrementing
- **File:** `src/RetailApp.Inventory/Services/InventoryService.cs`
- **Lines:** `108–109` (inside `Restock`, lines `98–112`)
- **Faulty Code:**
  ```csharp
  var updated = quantity;
  _stockLevels[product.Id] = updated;
  ```
- **Cause:** Assigns `quantity` directly instead of adding it to the existing stock (`_stockLevels.GetValueOrDefault(product.Id, 0) + quantity`).

### 7. RETAIL-7 — Product Search Is Case-Sensitive
- **File:** `src/RetailApp.Catalog/Services/ProductCatalogService.cs`
- **Lines:** `35–37` (inside `GetAll`, lines `31–38`)
- **Faulty Code:**
  ```csharp
  p.Name.Contains(term, StringComparison.Ordinal) ||
  p.Description.Contains(term, StringComparison.Ordinal) ||
  p.Id.Contains(term, StringComparison.Ordinal));
  ```
- **Cause:** Uses `StringComparison.Ordinal` instead of `StringComparison.OrdinalIgnoreCase`.

### 8. RETAIL-8 — Promo Code Discount Is Added to Order Total Instead of Subtracted
- **File:** `src/RetailApp.Pricing/Services/PricingService.cs`
- **Line:** `59` (inside `CalculateQuote`, lines `24–73`)
- **Faulty Code:**
  ```csharp
  var taxableAmount = Math.Max(0m, afterBulk + promoDiscount);
  ```
- **Cause:** Uses `+ promoDiscount` instead of `- promoDiscount`, causing promo codes (`SAVE10`, `VIP20`, `WELCOME5`) to increase the taxable amount, sales tax, and grand total.

### 9. RETAIL-9 — 5% Bulk Discount Not Applied When Cart Has Exactly 5 Items
- **File:** `src/RetailApp.Pricing/Services/PricingService.cs`
- **Line:** `52` (inside `CalculateQuote`, lines `24–73`)
- **Faulty Code:**
  ```csharp
  var bulkDiscount = totalQuantity > BulkDiscountMinQuantity
  ```
- **Cause:** Uses `>` instead of `>=` against `BulkDiscountMinQuantity` (`5`), so orders with exactly 5 units receive `$0.00` bulk discount.

### 10. RETAIL-10 — Cancelled Order Does Not Restore Reserved Inventory
- **File:** `src/RetailApp.Orders/Services/OrderService.cs`
- **Line:** `137` (inside `CancelOrder`, lines `135–138`)
- **Faulty Code:**
  ```csharp
  foreach (var item in cancelledOrder.Quote.Items)
  {
      _inventory.Release(item.ProductId, -item.Quantity);
  }
  ```
- **Cause:** Passes `-item.Quantity` (negative value) to `_inventory.Release(...)`. Because `InventoryService.Release` guards with `if (quantity <= 0) return;` (`src/RetailApp.Inventory/Services/InventoryService.cs`, lines `84–85`), the method exits early and never returns stock.
