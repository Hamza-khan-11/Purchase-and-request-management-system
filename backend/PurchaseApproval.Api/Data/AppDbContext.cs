using Microsoft.EntityFrameworkCore;
using PurchaseApproval.Api.Models;

namespace PurchaseApproval.Api.Data;

public partial class AppDbContext : DbContext
{
    private readonly PurchaseApproval.Api.Services.ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, PurchaseApproval.Api.Services.ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VendorProduct> VendorProducts => Set<VendorProduct>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<RequestItem> RequestItems => Set<RequestItem>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiRecommendationLog> AiRecommendationLogs => Set<AiRecommendationLog>();

    // Rules that apply to every column of a certain type
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();

        // Store enums as readable text (for example 'Draft') instead of numbers
        configurationBuilder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<RequestStatus>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<ApprovalDecision>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<PurchaseOrderStatus>().HaveConversion<string>().HaveMaxLength(30);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<Vendor>(e =>
        {
            e.Property(x => x.Code).IsRequired().HasMaxLength(20);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ContactPerson).HasMaxLength(150);
            e.Property(x => x.Email).IsRequired().HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.Address).HasMaxLength(500);
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasIndex(x => x.Name);
            e.ToTable(t => t.HasCheckConstraint("CK_Vendors_Rating", "[Rating] >= 0 AND [Rating] <= 5"));
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Sku).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Category).IsRequired().HasMaxLength(100);
            e.Property(x => x.Unit).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.Sku).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Category);
            e.ToTable(t => t.HasCheckConstraint("CK_Products_StandardPrice", "[StandardPrice] >= 0"));
        });

        modelBuilder.Entity<VendorProduct>(e =>
        {
            e.HasIndex(x => new { x.VendorId, x.ProductId }).IsUnique().HasFilter("[IsDeleted] = 0");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_VendorProducts_QuotedPrice", "[QuotedPrice] >= 0");
                t.HasCheckConstraint("CK_VendorProducts_LeadTimeDays", "[LeadTimeDays] >= 0");
            });
        });

        modelBuilder.Entity<PurchaseRequest>(e =>
        {
            e.Property(x => x.RequestNumber).IsRequired().HasMaxLength(30);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Justification).HasMaxLength(1000);
            e.HasIndex(x => x.RequestNumber).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasIndex(x => x.Status);
            e.ToTable(t => t.HasCheckConstraint("CK_PurchaseRequests_TotalAmount", "[TotalAmount] >= 0"));
        });

        modelBuilder.Entity<RequestItem>(e =>
        {
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.PurchaseRequestId, x.ProductId }).IsUnique().HasFilter("[IsDeleted] = 0");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_RequestItems_Quantity", "[Quantity] > 0");
                t.HasCheckConstraint("CK_RequestItems_UnitPrice", "[EstimatedUnitPrice] >= 0");
            });
        });

        modelBuilder.Entity<Approval>(e =>
        {
            e.Property(x => x.Comments).HasMaxLength(1000);
        });

        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.Property(x => x.PoNumber).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.PoNumber).IsUnique().HasFilter("[IsDeleted] = 0");
            // Only one active (not cancelled, not deleted) PO per request
            e.HasIndex(x => x.PurchaseRequestId).IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [Status] <> 'Cancelled'");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PurchaseOrders_TotalAmount", "[TotalAmount] >= 0");
                t.HasCheckConstraint("CK_PurchaseOrders_Dates", "[PromisedDeliveryDate] >= [OrderDate]");
            });
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).IsRequired().HasMaxLength(100);
            e.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
            e.HasIndex(x => new { x.EntityName, x.EntityId });
            e.HasIndex(x => x.Timestamp);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<AiRecommendationLog>(e =>
        {
            e.Property(x => x.OverrideReason).HasMaxLength(1000);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedBy);
            e.HasOne<Vendor>().WithMany().HasForeignKey(x => x.RecommendedVendorId);
            e.HasOne<Vendor>().WithMany().HasForeignKey(x => x.FinalVendorId);
            e.HasQueryFilter(x => !x.PurchaseRequest.IsDeleted);
        });
         
        // Soft delete: rows with IsDeleted = 1 are hidden from every normal query
        SoftDeleteFilter<User>(modelBuilder);
        SoftDeleteFilter<Vendor>(modelBuilder);
        SoftDeleteFilter<Product>(modelBuilder);
        SoftDeleteFilter<VendorProduct>(modelBuilder);
        SoftDeleteFilter<PurchaseRequest>(modelBuilder);
        SoftDeleteFilter<RequestItem>(modelBuilder);
        SoftDeleteFilter<Approval>(modelBuilder);
        SoftDeleteFilter<PurchaseOrder>(modelBuilder);

        // No automatic cascade deletes anywhere (we never hard delete)
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void SoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(x => !x.IsDeleted);
    }
}


