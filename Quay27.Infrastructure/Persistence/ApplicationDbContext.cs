using Microsoft.EntityFrameworkCore;
using Quay27.Domain.Entities;

namespace Quay27.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ColumnPermission> ColumnPermissions => Set<ColumnPermission>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Queue> Queues => Set<Queue>();
    public DbSet<CustomerQueue> CustomerQueues => Set<CustomerQueue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CustomerVersion> CustomerVersions => Set<CustomerVersion>();
    public DbSet<DuplicateFlag> DuplicateFlags => Set<DuplicateFlag>();
    public DbSet<SheetPickerDraftStaffName> SheetPickerDraftStaffNames => Set<SheetPickerDraftStaffName>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierGroup> SupplierGroups => Set<SupplierGroup>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<ReturnReceipt> ReturnReceipts => Set<ReturnReceipt>();
    public DbSet<ReturnReceiptLine> ReturnReceiptLines => Set<ReturnReceiptLine>();
    public DbSet<SupplierPaymentAllocation> SupplierPaymentAllocations => Set<SupplierPaymentAllocation>();
    public DbSet<ReceivingAccount> ReceivingAccounts => Set<ReceivingAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(128).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<ColumnPermission>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TableName).HasMaxLength(128).IsRequired();
            e.Property(x => x.ColumnName).HasMaxLength(128).IsRequired();
            e.HasIndex(x => new { x.UserId, x.TableName, x.ColumnName });
            e.HasOne(x => x.User).WithMany(u => u.ColumnPermissions).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.Property(x => x.InvoiceCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.NameAddress).HasColumnType("longtext").IsRequired();
            e.Property(x => x.CreateMachine).HasMaxLength(128);
            e.Property(x => x.DraftStaff).HasMaxLength(128);
            e.Property(x => x.InspectorStaff).HasMaxLength(128);
            e.Property(x => x.InstallStaffCm).HasMaxLength(128);
            e.Property(x => x.Notes).HasColumnType("longtext");
            e.Property(x => x.GoodsSenderNote).HasMaxLength(256);
            e.Property(x => x.AdditionalNotes).HasColumnType("longtext");
            e.Property(x => x.Status).HasMaxLength(128);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.SheetDate);
            e.HasIndex(x => x.InvoiceCode);
        });

        modelBuilder.Entity<Queue>(e =>
        {
            e.ToTable("Queues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<CustomerQueue>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.HasIndex(x => new { x.CustomerId, x.QueueId }).IsUnique();
            e.HasOne(x => x.Customer).WithMany(c => c.CustomerQueues).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Queue).WithMany(q => q.CustomerQueues).HasForeignKey(x => x.QueueId);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TableName).HasMaxLength(128).IsRequired();
            e.Property(x => x.ColumnName).HasMaxLength(128).IsRequired();
            // longtext: two large varchar(8000) utf8mb4 columns exceed MySQL ~64KB max row size for the table type
            e.Property(x => x.OldValue).HasColumnType("longtext");
            e.Property(x => x.NewValue).HasColumnType("longtext");
            e.Property(x => x.ActionType).HasMaxLength(64).IsRequired();
            e.Property(x => x.ChangedBy).HasMaxLength(256).IsRequired();
            e.HasIndex(x => new { x.TableName, x.RecordId });
        });

        modelBuilder.Entity<CustomerVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SnapshotData).HasColumnType("json");
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Customer).WithMany(c => c.CustomerVersions).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<DuplicateFlag>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DuplicateGroupId);
            e.HasOne(x => x.Customer).WithMany(c => c.DuplicateFlags).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<SheetPickerDraftStaffName>(e =>
        {
            e.ToTable("SheetPickerDraftStaffNames");
            e.HasKey(x => x.Id);
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.SortOrder);
        });

        modelBuilder.Entity<CustomerProfile>(e =>
        {
            e.ToTable("CustomerProfiles");
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomerCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone1).HasMaxLength(32);
            e.Property(x => x.Phone2).HasMaxLength(32);
            e.Property(x => x.Gender).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Facebook).HasMaxLength(512);
            e.Property(x => x.Address).HasColumnType("longtext");
            e.Property(x => x.ProvinceCity).HasMaxLength(128);
            e.Property(x => x.Ward).HasMaxLength(128);
            e.Property(x => x.CustomerGroup).HasMaxLength(128);
            e.Property(x => x.Note).HasColumnType("longtext");
            e.Property(x => x.BuyerType).HasMaxLength(32).IsRequired();
            e.Property(x => x.BuyerName).HasMaxLength(256);
            e.Property(x => x.TaxCode).HasMaxLength(64);
            e.Property(x => x.InvoiceAddress).HasColumnType("longtext");
            e.Property(x => x.InvoiceProvinceCity).HasMaxLength(128);
            e.Property(x => x.InvoiceWard).HasMaxLength(128);
            e.Property(x => x.IdentityNumber).HasMaxLength(32);
            e.Property(x => x.PassportNumber).HasMaxLength(32);
            e.Property(x => x.InvoiceEmail).HasMaxLength(256);
            e.Property(x => x.InvoicePhone).HasMaxLength(32);
            e.Property(x => x.BankName).HasMaxLength(128);
            e.Property(x => x.BankAccountNumber).HasMaxLength(64);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.CustomerCode).IsUnique();
            e.HasIndex(x => x.CustomerName);
            e.HasIndex(x => x.CreatedDate);
        });

        modelBuilder.Entity<CustomerGroup>(e =>
        {
            e.ToTable("CustomerGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Description).HasMaxLength(512);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.CreatedDate);
        });

        modelBuilder.Entity<ProductGroup>(e =>
        {
            e.ToTable("ProductGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId);
            e.HasIndex(x => x.IsDeleted);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.ItemType).HasMaxLength(32).IsRequired();
            e.Property(x => x.Barcode).HasMaxLength(64);
            e.Property(x => x.Brand).HasMaxLength(128);
            e.Property(x => x.Location).HasMaxLength(128);
            e.Property(x => x.RowStatus).HasMaxLength(32).IsRequired();
            e.Property(x => x.Description).HasColumnType("longtext");
            e.Property(x => x.DescriptionRichText).HasColumnType("longtext");
            e.Property(x => x.InvoiceNoteTemplate).HasColumnType("longtext");
            e.Property(x => x.SupplierName).HasMaxLength(128);
            e.Property(x => x.ImageUrl).HasMaxLength(1024);
            e.HasOne(x => x.Group).WithMany(x => x.Products).HasForeignKey(x => x.GroupId);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Barcode).IsUnique();
            e.HasIndex(x => new { x.Name, x.GroupId, x.RowStatus, x.DirectSale });
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.IsDeleted);
        });

        modelBuilder.Entity<PriceList>(e =>
        {
            e.ToTable("PriceLists");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.FormulaSource).HasMaxLength(32).IsRequired();
            e.Property(x => x.FormulaOperation).HasMaxLength(32).IsRequired();
            e.Property(x => x.FormulaUnit).HasMaxLength(16).IsRequired();
            e.Property(x => x.SalesRuleMode).HasMaxLength(32).IsRequired();
            e.Property(x => x.BranchIdsJson).HasColumnType("longtext").IsRequired();
            e.Property(x => x.CustomerGroupIdsJson).HasColumnType("longtext").IsRequired();
            e.Property(x => x.CashierIdsJson).HasColumnType("longtext").IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.StartAt, x.EndAt });
        });

        modelBuilder.Entity<PriceListItem>(e =>
        {
            e.ToTable("PriceListItems");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.PriceList)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PriceListId, x.ProductId }).IsUnique();
            e.HasIndex(x => x.ProductId);
        });

        modelBuilder.Entity<SupplierGroup>(e =>
        {
            e.ToTable("SupplierGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Notes).HasColumnType("longtext");
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("Suppliers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Address).HasColumnType("longtext");
            e.Property(x => x.Region).HasMaxLength(256);
            e.Property(x => x.Ward).HasMaxLength(256);
            e.Property(x => x.Notes).HasColumnType("longtext");
            e.Property(x => x.CompanyName).HasMaxLength(256);
            e.Property(x => x.TaxCode).HasMaxLength(32);
            e.Property(x => x.InitialDebt).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPurchase).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalReturn).HasColumnType("decimal(18,2)");
            e.Property(x => x.CurrentDebt).HasColumnType("decimal(18,2)");
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Phone);
            e.HasOne(x => x.SupplierGroup)
                .WithMany(g => g.Suppliers)
                .HasForeignKey(x => x.SupplierGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReceivingAccount>(e =>
        {
            e.ToTable("ReceivingAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.AccountNumber).HasMaxLength(64).IsRequired();
            e.Property(x => x.BankName).HasMaxLength(256);
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.Name, x.AccountNumber });
        });

        modelBuilder.Entity<GoodsReceipt>(e =>
        {
            e.ToTable("GoodsReceipts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.SupplierDebtDelta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Notes).HasColumnType("longtext");
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.ReceiptDate);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceiptLine>(e =>
        {
            e.ToTable("GoodsReceiptLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCodeSnapshot).HasMaxLength(64).IsRequired();
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(256).IsRequired();
            e.Property(x => x.UnitSnapshot).HasMaxLength(64);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.GoodsReceipt)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReturnReceipt>(e =>
        {
            e.ToTable("ReturnReceipts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.SupplierPaidAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.SupplierDebtDelta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Notes).HasColumnType("longtext");
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.ReturnDate);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReturnReceiptLine>(e =>
        {
            e.ToTable("ReturnReceiptLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCodeSnapshot).HasMaxLength(64).IsRequired();
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(256).IsRequired();
            e.Property(x => x.UnitSnapshot).HasMaxLength(64);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.ImportPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.ReturnPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.ReturnReceipt)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.ReturnReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierPaymentAllocation>(e =>
        {
            e.ToTable("SupplierPaymentAllocations");
            e.HasKey(x => x.Id);
            e.Property(x => x.PaymentMethod).HasMaxLength(32).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.GoodsReceipt)
                .WithMany(x => x.PaymentAllocations)
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReturnReceipt)
                .WithMany(x => x.PaymentAllocations)
                .HasForeignKey(x => x.ReturnReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReceivingAccount)
                .WithMany()
                .HasForeignKey(x => x.ReceivingAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
