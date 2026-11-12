using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Already seeded? Then do nothing.
        if (await db.Users.IgnoreQueryFilters().AnyAsync()) return;

        var now = DateTime.UtcNow;
        var today = now.Date;
        var hasher = new PasswordHasher<User>();

        // ---------- Users (passwords are stored as hashes) ----------
        User MakeUser(string name, string email, UserRole role, string password)
        {
            var u = new User { FullName = name, Email = email, Role = role, CreatedAt = now };
            u.PasswordHash = hasher.HashPassword(u, password);
            return u;
        }
        var admin = MakeUser("Demo Admin", "admin@demo.com", UserRole.Admin, "Admin@123");
        var manager = MakeUser("Demo Manager", "manager@demo.com", UserRole.Manager, "Manager@123");
        var officer = MakeUser("Demo Officer", "officer@demo.com", UserRole.ProcurementOfficer, "Officer@123");
        var req1 = MakeUser("Demo Requester One", "requester1@demo.com", UserRole.Requester, "Requester@123");
        var req2 = MakeUser("Demo Requester Two", "requester2@demo.com", UserRole.Requester, "Requester@123");
        db.Users.AddRange(admin, manager, officer, req1, req2);

        // ---------- Vendors ----------
        string[] vendorNames =
        {
            "TechSource Supplies", "OfficeHub Traders", "Prime Electronics", "Alpha Stationers",
            "Metro Supplies", "BlueLine Computers", "SwiftPrint Solutions", "BudgetMart Wholesale",
            "Everest Office Systems", "Summit IT Distributors", "Paper Plus Stationery", "Crescent Furniture"
        };
        decimal[] ratings = { 4.8m, 4.5m, 4.2m, 4.0m, 4.6m, 3.4m, 4.3m, 3.0m, 3.8m, 4.4m, 4.1m, 3.9m };
        var vendors = new List<Vendor>();
        for (int i = 0; i < vendorNames.Length; i++)
        {
            vendors.Add(new Vendor
            {
                Code = $"V{i + 1:000}",
                Name = vendorNames[i],
                ContactPerson = $"Sales Contact {i + 1}",
                Email = $"contact@vendor{i + 1:00}.example",
                Phone = $"555-01{i + 1:00}",
                Address = $"{100 + i} Market Street",
                Rating = ratings[i],
                CreatedAt = now
            });
        }
        db.Vendors.AddRange(vendors);

        var productData = new (string Sku, string Name, string Category, string Unit, decimal Price)[]
        {
            ("LAP-14", "Laptop 14-inch", "IT Equipment", "Piece", 900m),
            ("DSK-PC1", "Desktop Computer", "IT Equipment", "Piece", 700m),
            ("MON-24", "Monitor 24-inch", "IT Equipment", "Piece", 180m),
            ("MOU-W1", "Wireless Mouse", "IT Accessories", "Piece", 15m),
            ("KEY-M1", "Mechanical Keyboard", "IT Accessories", "Piece", 45m),
            ("PAP-A4", "A4 Paper Ream", "Stationery", "Ream", 5m),
            ("TON-01", "Printer Toner Cartridge", "Consumables", "Piece", 60m),
            ("CHR-01", "Office Chair", "Furniture", "Piece", 120m),
            ("DSK-ST1", "Standing Desk", "Furniture", "Piece", 300m),
            ("WEB-HD", "HD Webcam", "IT Accessories", "Piece", 40m),
            ("HDS-01", "Headset", "IT Accessories", "Piece", 35m),
            ("NTB-01", "Notebook Pack", "Stationery", "Box", 12m)
        };
        var products = productData.Select(d => new Product
        {
            Sku = d.Sku, Name = d.Name, Category = d.Category, Unit = d.Unit,
            StandardPrice = d.Price, CreatedAt = now
        }).ToList();
        db.Products.AddRange(products);

        // ---------- Vendor price quotes ----------
        // Vendors 0-8 sell everything; 9 = IT only; 10 = stationery/consumables only; 11 = furniture only
        static bool Quotes(int v, int p) => v switch
        {
            9 => p is 0 or 1 or 2 or 3 or 4 or 9 or 10,
            10 => p is 5 or 6 or 11,
            11 => p is 7 or 8,
            _ => true
        };
        decimal[] priceBias = { 1.00m, 1.05m, 1.08m, 0.95m, 1.10m, 0.90m, 1.02m, 0.88m, 0.97m, 1.04m, 0.93m, 1.00m };
        var quotes = new Dictionary<(int v, int p), VendorProduct>();
        for (int v = 0; v < vendors.Count; v++)
        {
            for (int p = 0; p < products.Count; p++)
            {
                if (!Quotes(v, p)) continue;
                decimal noise = (((v * 7 + p * 3) % 9) - 4) / 100m;
                var vp = new VendorProduct
                {
                    Vendor = vendors[v],
                    Product = products[p],
                    QuotedPrice = Math.Round(productData[p].Price * priceBias[v] * (1 + noise), 2),
                    LeadTimeDays = 3 + (v * 5 + p * 2) % 12,
                    CreatedAt = now
                };
                quotes[(v, p)] = vp;
                db.VendorProducts.Add(vp);
            }
        }

        // ---------- Helpers to build requests, approvals and orders ----------
        string[] titles =
        {
            "Laptops for new team members", "Office stationery restock", "Monitor upgrade",
            "Ergonomic chairs", "Printer supplies", "Workstation accessories"
        };
        int reqSeq = 0, poSeq = 0;

        PurchaseRequest NewRequest(int k, User requester, RequestStatus status, DateTime createdAt)
        {
            reqSeq++;
            var req = new PurchaseRequest
            {
                RequestNumber = $"PR-{createdAt.Year}-{reqSeq:0000}",
                Requester = requester,
                Title = titles[k % titles.Length],
                Justification = "Needed for day-to-day operations.",
                Status = status,
                RequiredByDate = createdAt.AddDays(21),
                CreatedAt = createdAt
            };
            int itemCount = 1 + (k % 3);
            decimal total = 0;
            for (int m = 0; m < itemCount; m++)
            {
                int p = (k * 2 + m * 3) % products.Count;
                int qty = 1 + (k * 3 + m * 5) % 20;
                decimal unit = productData[p].Price;
                decimal line = qty * unit;
                total += line;
                req.Items.Add(new RequestItem
                {
                    Product = products[p], Quantity = qty, EstimatedUnitPrice = unit,
                    LineTotal = line, CreatedAt = createdAt
                });
            }
            req.TotalAmount = total;
            db.PurchaseRequests.Add(req);
            return req;
        }

        void AddApproval(PurchaseRequest req, User approver, ApprovalDecision decision, string comment, DateTime at)
        {
            db.Approvals.Add(new Approval
            {
                PurchaseRequest = req, Approver = approver, Decision = decision,
                Comments = comment, DecidedAt = at, CreatedAt = at
            });
        }

        void AddPo(PurchaseRequest req, int v, DateTime orderDate, int leadDays, PurchaseOrderStatus status, DateTime? delivered)
        {
            poSeq++;
            decimal total = req.Items.Sum(i => i.Quantity * quotes[(v, products.IndexOf(i.Product))].QuotedPrice);
            db.PurchaseOrders.Add(new PurchaseOrder
            {
                PoNumber = $"PO-{orderDate.Year}-{poSeq:0000}",
                PurchaseRequest = req,
                Vendor = vendors[v],
                OrderDate = orderDate,
                PromisedDeliveryDate = orderDate.AddDays(leadDays),
                DeliveredDate = delivered,
                TotalAmount = total,
                Status = status,
                CreatedAt = orderDate
            });
        }

        // ---------- History: 36 delivered orders (vendors 0-8, four each) ----------
        // How many of each vendor's 4 orders were late (vendor 7 is cheap but unreliable)
        int[] lateOf4 = { 0, 0, 1, 1, 0, 2, 0, 3, 1 };
        for (int k = 0; k < 36; k++)
        {
            int v = k % 9;
            int j = k / 9;
            var created = today.AddDays(-330 + k * 9);
            var req = NewRequest(k, k % 2 == 0 ? req1 : req2, RequestStatus.PoCreated, created);
            req.SubmittedAt = created.AddHours(2);
            AddApproval(req, manager, ApprovalDecision.Approved, "Approved.", created.AddDays(1));

            var orderDate = created.AddDays(2);
            int lead = 7 + (k % 5);
            var promised = orderDate.AddDays(lead);
            var delivered = j < lateOf4[v] ? promised.AddDays(2 + 2 * j) : promised.AddDays(-(j % 3));
            AddPo(req, v, orderDate, lead, PurchaseOrderStatus.Delivered, delivered);
        }

        // ---------- Current requests in different statuses ----------
        var current = new[]
        {
            RequestStatus.Draft, RequestStatus.Draft, RequestStatus.Draft,
            RequestStatus.Submitted, RequestStatus.Submitted, RequestStatus.Submitted, RequestStatus.Submitted,
            RequestStatus.Approved, RequestStatus.Approved, RequestStatus.Approved,
            RequestStatus.Rejected, RequestStatus.Rejected,
            RequestStatus.Cancelled,
            RequestStatus.PoCreated, RequestStatus.PoCreated
        };
        for (int c = 0; c < current.Length; c++)
        {
            var status = current[c];
            var created = today.AddDays(-30 + c * 2);
            var req = NewRequest(36 + c, c % 2 == 0 ? req1 : req2, status, created);

            if (status != RequestStatus.Draft) req.SubmittedAt = created.AddHours(3);
            if (status == RequestStatus.Approved || status == RequestStatus.PoCreated)
                AddApproval(req, manager, ApprovalDecision.Approved, "Approved for purchase.", created.AddDays(1));
            if (status == RequestStatus.Rejected)
                AddApproval(req, manager, ApprovalDecision.Rejected, "Budget not available this quarter.", created.AddDays(1));
            if (status == RequestStatus.PoCreated)
            {
                var poStatus = c == 14 ? PurchaseOrderStatus.Sent : PurchaseOrderStatus.Created;
                AddPo(req, c == 13 ? 2 : 4, created.AddDays(2), 10, poStatus, null);
            }
        }

        await db.SaveChangesAsync();
    }
}
