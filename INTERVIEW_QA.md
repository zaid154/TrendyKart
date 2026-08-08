# TrendyKart — Technical Interview Questions & Answers

This document contains in-depth technical questions and answers about the architecture, design choices, challenges, and implementation details of **TrendyKart**, designed for technical interviews.

---

## 1. Architecture & Design Patterns

### Q1: Why did you choose ASP.NET Core 10 MVC for TrendyKart?
**Answer:**
ASP.NET Core 10 MVC was selected because of its high performance, strong type safety, cross-platform capabilities, and out-of-the-box support for Dependency Injection and Middleware pipeline configuration. For an e-commerce platform requiring strict separation of concerns (Models for data & business validation, Controllers for request handling & orchestration, Views for Razor rendering), MVC provides a clear, maintainable architecture that keeps data structures, admin controllers, and frontend view templates cleanly organized.

### Q2: How did you implement multi-variant product support in Entity Framework Core?
**Answer:**
We decoupled the core product definition from its physical purchasing specifications:
- `Product` represents the top-level entity containing general metadata (Name, Description, SubCategoryID, Brand, Base Image, Rating).
- `ProductVariant` represents specific purchasable SKUs (e.g. 128GB/8GB RAM vs 256GB/12GB RAM) containing individual `Price`, `OldPrice`, `Stock`, `SKU`, `ColorName`, `SpecificationsJson`, and `AttributesJson`.
- `ProductMedia` links to either a `Product` or an individual `ProductVariant`, allowing variant-specific photo/video galleries.
- In EF Core, we configured one-to-many navigation properties with cascade deletes on Products and `NoAction` on variant media references to prevent circular delete cascades in SQL Server.

---

## 2. Dynamic Filtering & Database Optimization

### Q3: How does the dynamic attribute filtering system work when searching for products?
**Answer:**
Traditional e-commerce platforms hardcode category filters. In TrendyKart, we designed an attribute-aware dynamic filtering engine:
1. `CategoryFilterAttribute` defines customizable attributes per Sub-Category (e.g. Mobiles -> RAM, Storage; Laptops -> Screen Size, Processor) with JSON option arrays.
2. When a user selects a Category or inputs a search keyword, `ProductController.Index` auto-detects the matching Sub-Category and loads its dynamic `CategoryFilterAttribute` definitions into `ViewBag.FilterAttributes`.
3. When query string parameters like `attr_RAM=16GB` are passed, LINQ filters product variants by checking both `SpecificationsJson` and `AttributesJson` via `Contains()` queries on EF Core.
4. Eager loading (`.Include(p => p.Variants).Include(p => p.SubCategory)`) is used to prevent N+1 query performance problems during listing generation.

---

## 3. Financial & Business Logic Engine

### Q4: How is GST calculated across different product categories?
**Answer:**
We implemented a hierarchical GST resolution engine via `IGSTCalculatorService`:
1. **GST Hierarchy**: Product Level `GSTOverridePercentage` (if set) > Sub-Category `GSTPercentage` (if set) > Category `GSTPercentage` > Default (18%).
2. **Tax Breakdown**: Because retail prices are shown inclusive of tax to consumers, the base price and GST component are computed mathematically:
   $$\text{Base Price} = \text{Round}\left(\frac{\text{Item Price} \times \text{Quantity}}{1 + \frac{\text{GST \%}}{100}}, 2\right)$$
   $$\text{GST Amount} = (\text{Item Price} \times \text{Quantity}) - \text{Base Price}$$
3. The exact breakdown (Base SubTotal, GST Total, Shipping Charge, and Coupon Discount) is stored in the `Order` and `OrderItem` database tables to ensure historical invoice integrity even if category GST rates change in the future.

### Q5: How does the Coupon validation engine prevent unauthorized usage?
**Answer:**
The `CouponService` validates several rules sequentially:
1. **Active & Expiry Window Check**: Verifies `IsActive` flag and `StartDate` / `EndDate` bounds against UTC time.
2. **Minimum Order Value**: Ensures order subtotal meets `MinOrderAmount`.
3. **Usage Type & First-Order Constraint**: If `UsageType == "FirstOrderOnly"`, queries the `Orders` table for any existing non-cancelled orders by the logged-in `CustomerID`.
4. **Usage Limits**: Verifies global `TotalUsageLimit` against `TimesUsed` and per-user limits against the user's order history.
5. **Discount Calculation & Capping**: Percentage discounts compute $\text{SubTotal} \times \frac{\text{DiscountValue}}{100}$ and cap the result at `MaxDiscountCap` if specified.

---

## 4. Payment Gateway & Security

### Q6: How is Razorpay payment security ensured during checkout?
**Answer:**
1. **Order Creation**: Before opening the checkout modal, the frontend invokes `CartController.CreateRazorpayOrder` to generate a server-side Razorpay Order ID.
2. **Client-Side Modal**: Razorpay's JS SDK opens the modal with the pre-calculated amount in paisa.
3. **Signature Verification**: On payment completion, Razorpay returns `razorpay_order_id`, `razorpay_payment_id`, and `razorpay_signature`.
4. **Server Verification**: `RazorpayService.VerifySignature` computes the HMAC-SHA256 hash:
   $$\text{HMAC-SHA256}(\text{razorpay\_order\_id} + "|" + \text{razorpay\_payment\_id}, \text{key\_secret})$$
   The order status is updated to `Paid` only if the calculated hash matches `razorpay_signature`, preventing tampering or fake client-side success requests.

---

## 5. File Validation & PDF Invoice System

### Q7: How are media uploads validated and stored safely?
**Answer:**
Media uploads undergo dual-layer validation:
- **Client-Side**: File size check (Images ≤ 5MB, Videos ≤ 10MB) and MIME extension filtering.
- **Server-Side**: File extension inspection (`.jpg`, `.png`, `.webp`, `.avif`, `.mp4`, `.webm`) and byte-length verification (`file.Length <= maxAllowed`). Files are stored under `wwwroot/uploads/` with cryptographically random `Guid` filenames to prevent overwrite collisions or path traversal attacks.

### Q8: How does the Authorized Signature Stamp work in generated invoices?
**Answer:**
Admin users can upload an authorized signature image stamp via Site Settings (`SiteSetting.AuthorizedSignatureUrl`). `InvoicePdfService` embeds this signature dynamically onto tax invoice templates, displaying company credentials, itemized GST breakdowns, and printable PDF styling suitable for direct downloading or automated email dispatch.
