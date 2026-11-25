# RetailApp — Step-by-Step Navigation & Route Guide to Reproduce Each Bug

## Starting the Application

Run the following command from the project root directory:

```powershell
dotnet run --project src/RetailApp.WebApi/RetailApp.WebApi.csproj
```

Open the URL displayed in the terminal (for example `http://localhost:5000` or the port printed by ASP.NET Core) in your browser. All routes below are relative to that base URL (e.g., `http://localhost:5000`).

---

## RETAIL-1 — Navigation Bar "Orders" Link Redirects to Catalog
- **Page / Route:** `/` (Storefront Home Page — Top Navigation Bar)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Scroll slightly down or note the **Recent Orders** card below the Product Catalog.
  3. In the dark top navigation bar, click **Orders**.
- **What You Will See:** The active tab switches to **Catalog** and the page scrolls to / highlights **Product Catalog & Live Stock** instead of **Recent Orders**.

---

## RETAIL-2 — Valid Credentials Fail on Sign In
- **Page / Route:** `/` -> Top Navigation Bar -> **Sign In** modal (API route: `POST /api/auth/login`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Click the **Sign In** button on the right side of the top navigation bar.
  3. In the Sign In modal, enter the demo credentials displayed on screen:
     - **Email:** `alex@retailapp.com`
     - **Password:** `Shop@2026`
  4. Click **Sign In**.
- **What You Will See:** A red error alert appears saying `Invalid email or password.` (HTTP `401`) even though the exact valid credentials were entered.

---

## RETAIL-3 — Cart Minus (`-`) Button Increases Quantity
- **Page / Route:** `/` -> **Shopping Cart** panel (right column)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Under **Product Catalog & Live Stock**, click **Add to Cart** on any product (for example, *Classic Cotton Hoodie*).
  3. Look at the **Shopping Cart** panel on the right — the quantity shows `1`.
  4. Click the **`-`** button next to the item in the cart.
- **What You Will See:** Instead of decreasing the quantity to `0` / removing the item, the quantity **increases** to `2` (and continues increasing on each click).

---

## RETAIL-4 — "Reset Filters" Button Does Not Clear Category or Reload Products
- **Page / Route:** `/` -> **Product Catalog & Live Stock** -> Filter Bar
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. In the **Product Catalog & Live Stock** filter bar:
     - Select **Electronics** from the category dropdown.
     - Type `Keyboard` into the search input box (only *Mechanical Compact Keyboard* is displayed).
  3. Click the **Reset Filters** button.
- **What You Will See:** The search box clears, but the category dropdown remains stuck on **Electronics** and the product grid does **not** reload — it stays stuck showing only *Mechanical Compact Keyboard*.

---

## RETAIL-5 — "View Receipt" on Confirmed Order Triggers HTTP 500 Internal Server Error
- **Page / Route:** `/` -> **Recent Orders** section -> **View Receipt** button (API route: `GET /api/orders/{orderId}/receipt`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Click **Add to Cart** on any product (for example, *Everyday Running Sneakers*).
  3. In the **Shopping Cart** panel, click **Place Order** to create a confirmed order (e.g., `ORD-1001`).
  4. Scroll down to the **Recent Orders** section.
  5. Click the **View Receipt** button on the newly created confirmed order.
- **What You Will See:** An error banner displays `HTTP 500 Internal Server Error: Unable to load receipt for order ORD-1001.` (and the browser Network tab confirms status `500` from `/api/orders/ORD-1001/receipt`).

---

## RETAIL-6 — "+5 Stock" Overwrites Available Stock to 5 Instead of Adding 5
- **Page / Route:** `/` -> **Product Catalog & Live Stock** -> Product Card **+5 Stock** button (API route: `POST /api/inventory/{productId}/restock`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Locate *Wireless Noise-Canceling Headphones* (`SKU-1001`), which initially shows **12 in stock** (or any item with more than 5 units).
  3. Click the **+5 Stock** button on that product card.
- **What You Will See:** A green toast says `Restocked 5 units of SKU-1001.`, but the product badge drops from **12 in stock** down to **5 in stock** (and turns yellow as low stock) instead of increasing to **17 in stock**.

---

## RETAIL-7 — Product Search Fails for Lowercase Queries
- **Page / Route:** `/` -> **Product Catalog & Live Stock** -> Search Box (API route: `GET /api/products?search=wireless`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Make sure the category dropdown is set to **All Categories**.
  3. In the **Search products...** input box, type `wireless` (all lowercase) or `sku-1001`.
  4. Then clear it and type `Wireless` (capital `W`) or `SKU-1001`.
- **What You Will See:** Searching `wireless` or `sku-1001` displays `No products match your search criteria.`, while `Wireless` or `SKU-1001` finds *Wireless Noise-Canceling Headphones*.

---

## RETAIL-8 — Applying a Promo Code Increases the Order Total Instead of Discounting It
- **Page / Route:** `/` -> **Shopping Cart** -> Promo Code Input & **Apply** Button (API route: `POST /api/pricing/quote`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Click **Add to Cart** on *Classic Cotton Hoodie* (`$48.00`).
  3. Note the initial Cart totals: Subtotal `$48.00`, Tax `$3.84`, Total **`$51.84`**.
  4. Enter `SAVE10` in the **Promo code** field and click **Apply**.
- **What You Will See:** The summary shows `Promo Discount: -$4.80 (SAVE10)`, yet Tax jumps to `$4.22` and Total **increases** to **`$57.02`** instead of decreasing to `$46.66`.

---

## RETAIL-9 — 5% Bulk Discount Not Applied for Exactly 5 Items
- **Page / Route:** `/` -> **Shopping Cart** -> Totals Summary (API route: `POST /api/pricing/quote`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Clear any promo code field.
  3. Click **Add to Cart** 5 times on *Classic Cotton Hoodie* (`$48.00`) so the cart quantity is **5** (Subtotal `$240.00`).
  4. Check the **Bulk Discount (5+ qty)** line in the Shopping Cart summary.
  5. Click **+** once more to bring quantity to **6** to compare.
- **What You Will See:** At quantity `5`, **Bulk Discount (5+ qty)** shows `-$0.00` instead of `-$12.00`. It only activates once quantity reaches `6`.

---

## RETAIL-10 — Cancelling an Order Does Not Restore Stock to Inventory
- **Page / Route:** `/` -> **Recent Orders** -> **Cancel** Button (API route: `POST /api/orders/{orderId}/cancel`)
- **Steps to Reach the Bug:**
  1. Open `/` in your browser.
  2. Note the current stock count of *Everyday Running Sneakers* (`SKU-2001`), initially **15 in stock**.
  3. Click **Add to Cart** twice on *Everyday Running Sneakers* (quantity `2`) and click **Place Order**.
  4. Confirm the stock count drops to **13 in stock**.
  5. Scroll to **Recent Orders** and click the red **Cancel** button on that order.
- **What You Will See:** The status changes to **Cancelled** and a green message says `Order ORD-xxxx cancelled and inventory restored.`, but *Everyday Running Sneakers* remains stuck at **13 in stock** instead of returning to **15 in stock**.
