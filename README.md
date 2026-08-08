# TrendyKart

TrendyKart is an e-commerce web application built using ASP.NET Core MVC and SQL Server. It includes product variant management (colors, storage options), dynamic category filters, shopping cart, checkout with Razorpay integration, admin panel, and order management.

## Developer

- **Name**: Mohd Zaid
- **Email**: Zaidm1323@gmail.com
- **LinkedIn**: https://www.linkedin.com/in/mohd-zaid-794090231/
- **GitHub**: https://github.com/zaid154
- **Portfolio**: https://portfolio-zeta-drab-97.vercel.app/

---

## Key Features

- **Product Variants**: Multi-variant support (color swatches, storage, RAM) with live price and image updates.
- **Dynamic Category & Filters**: Filter products by sub-category, brand, price range, and specs.
- **Admin Panel**: Manage products, variants, categories, coupons, orders, and customer accounts.
- **Cart & Checkout**: Cart management, GST calculations, Razorpay integration, and PDF invoice generation.

---

## Tech Stack

- **Backend**: ASP.NET Core 10 MVC
- **Database**: SQL Server + Entity Framework Core
- **Frontend**: HTML5, CSS, Bootstrap, JavaScript
- **Payment Gateway**: Razorpay

---

## How to Run the Project

1. Make sure **SQL Server** is running on your machine.
2. Update the connection string in `appsettings.json` if needed:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=TrendyKartDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connection Timeout=60"
   }
   ```
3. Open terminal in the project directory (`TrendyKart-old`) and run:
   ```bash
   dotnet run --project TrendyKart.csproj
   ```
   *(Note: Since there are multiple project files in the workspace, `--project TrendyKart.csproj` must be specified).*

4. Open your browser and go to:
   - **Website**: http://localhost:5159
   - **Admin Login**: http://localhost:5159/Admin/Login

5. **Admin Login Credentials**:
   - **Email**: `trendykart.app@gmail.com`
   - **Password**: `123456`

---

## Troubleshooting

- **MSB1011 Error**: If you run `dotnet run` without specifying `--project`, MSBuild fails because multiple project files exist. Run `dotnet run --project TrendyKart.csproj` instead.
- **CS2012 Error**: If the DLL is locked by a running instance, stop the previous running process or restart terminal before running again.
