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
    public DbSet<SupplierDebtAdjustment> SupplierDebtAdjustments => Set<SupplierDebtAdjustment>();
    public DbSet<SupplierPayablePayment> SupplierPayablePayments => Set<SupplierPayablePayment>();
    public DbSet<SupplierPayablePaymentLine> SupplierPayablePaymentLines => Set<SupplierPayablePaymentLine>();
    public DbSet<SupplierPayableDiscount> SupplierPayableDiscounts => Set<SupplierPayableDiscount>();
    public DbSet<SupplierPayableDiscountLine> SupplierPayableDiscountLines => Set<SupplierPayableDiscountLine>();
    public DbSet<ReceivingAccount> ReceivingAccounts => Set<ReceivingAccount>();
    public DbSet<BankCatalogItem> BankCatalogItems => Set<BankCatalogItem>();
    public DbSet<EWalletCatalogItem> EWalletCatalogItems => Set<EWalletCatalogItem>();
    public DbSet<SaleChannel> SaleChannels => Set<SaleChannel>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<SalesReturnExchangeItem> SalesReturnExchangeItems => Set<SalesReturnExchangeItem>();
    public DbSet<PaymentCategory> PaymentCategories => Set<PaymentCategory>();
    public DbSet<CashbookParty> CashbookParties => Set<CashbookParty>();
    public DbSet<CashbookEntry> CashbookEntries => Set<CashbookEntry>();
    public DbSet<CustomerInvoiceLine> CustomerInvoiceLines => Set<CustomerInvoiceLine>();

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
            e.Property(x => x.ManualCurrentDebt).HasPrecision(18, 4);
            e.Property(x => x.RewardPointsBalance).HasPrecision(18, 4).HasDefaultValue(0m);
            e.Property(x => x.RewardPointsLifetime).HasPrecision(18, 4).HasDefaultValue(0m);
            e.Property(x => x.IsActive).HasDefaultValue(true);
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
            e.Property(x => x.DiscountAmount).HasPrecision(18, 4);
            e.Property(x => x.DiscountIsPercent).HasDefaultValue(false);
            e.Property(x => x.RulesJson).HasColumnType("longtext").IsRequired();
            e.Property(x => x.CombineAllConditions).HasDefaultValue(true);
            e.Property(x => x.MembershipUpdateMode).HasMaxLength(16).IsRequired().HasDefaultValue("none");
            e.Property(x => x.IsAutoMembershipSync).HasDefaultValue(false);
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
            e.Property(x => x.AccountKind).HasMaxLength(32).IsRequired();
            e.Property(x => x.ProviderCode).HasMaxLength(128);
            e.Property(x => x.Note).HasColumnType("longtext");
            e.Property(x => x.ScopeKind).HasMaxLength(32).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.Name, x.AccountNumber });
            e.HasIndex(x => x.AccountKind);
        });

        modelBuilder.Entity<BankCatalogItem>(e =>
        {
            e.ToTable("BankCatalogItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(128).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(512).IsRequired();
            e.Property(x => x.GlobalName).HasMaxLength(512);
            e.Property(x => x.SearchText).HasMaxLength(1024).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<EWalletCatalogItem>(e =>
        {
            e.ToTable("EWalletCatalogItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(128).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(256).IsRequired();
            e.Property(x => x.GlobalName).HasMaxLength(256);
            e.Property(x => x.SearchText).HasMaxLength(512).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
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
            e.Property(x => x.SupplierPayableDiscountPortion).HasColumnType("decimal(18,2)");
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
            e.Property(x => x.Note).HasMaxLength(500);
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
            e.Property(x => x.Note).HasMaxLength(500);
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

        modelBuilder.Entity<SupplierDebtAdjustment>(e =>
        {
            e.ToTable("SupplierDebtAdjustments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Delta).HasColumnType("decimal(18,2)");
            e.Property(x => x.Description).HasColumnType("longtext");
            e.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => new { x.SupplierId, x.OccurredAtUtc });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierPayablePayment>(e =>
        {
            e.ToTable("SupplierPayablePayments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.PaymentMethod).HasMaxLength(32).IsRequired();
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Note).HasColumnType("longtext");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => new { x.SupplierId, x.OccurredAtUtc });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PayerUser)
                .WithMany()
                .HasForeignKey(x => x.PayerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReceivingAccount)
                .WithMany()
                .HasForeignKey(x => x.ReceivingAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CashbookEntry)
                .WithMany()
                .HasForeignKey(x => x.CashbookEntryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SupplierPayablePaymentLine>(e =>
        {
            e.ToTable("SupplierPayablePaymentLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Payment)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GoodsReceipt)
                .WithMany()
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierPayableDiscount>(e =>
        {
            e.ToTable("SupplierPayableDiscounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Note).HasColumnType("longtext");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => new { x.SupplierId, x.OccurredAtUtc });
            e.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PerformerUser)
                .WithMany()
                .HasForeignKey(x => x.PerformerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierPayableDiscountLine>(e =>
        {
            e.ToTable("SupplierPayableDiscountLines");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Discount)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.DiscountId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GoodsReceipt)
                .WithMany()
                .HasForeignKey(x => x.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleChannel>(e =>
        {
            e.ToTable("SaleChannels");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Description).HasColumnType("longtext");
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("PurchaseOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.CustomerCode).HasMaxLength(64);
            e.Property(x => x.CustomerName).HasMaxLength(256);
            e.Property(x => x.Note).HasColumnType("longtext");
            e.Property(x => x.DeliveryPartner).HasMaxLength(256);
            e.Property(x => x.ProvinceKey).HasMaxLength(64);
            e.Property(x => x.DistrictKey).HasMaxLength(64);
            e.Property(x => x.PaymentMethod).HasMaxLength(32).IsRequired();
            e.Property(x => x.SubtotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.AmountDue).HasColumnType("decimal(18,2)");
            e.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasOne(x => x.CustomerProfile).WithMany().HasForeignKey(x => x.CustomerProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ReceivedByUser).WithMany().HasForeignKey(x => x.ReceivedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SellerUser).WithMany().HasForeignKey(x => x.SellerUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SaleChannel).WithMany().HasForeignKey(x => x.SaleChannelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PurchaseOrderItem>(e =>
        {
            e.ToTable("PurchaseOrderItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ProductName).HasMaxLength(512).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.PurchaseOrder).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesInvoice>(e =>
        {
            e.ToTable("SalesInvoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.ReturnReferenceCode).HasMaxLength(64);
            e.Property(x => x.CustomerCode).HasMaxLength(64);
            e.Property(x => x.CustomerName).HasMaxLength(256);
            e.Property(x => x.Note).HasColumnType("longtext");
            e.Property(x => x.InvoiceDeliveryType).HasMaxLength(32).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.DeliveryStatus).HasMaxLength(64);
            e.Property(x => x.DeliveryPartner).HasMaxLength(256);
            e.Property(x => x.ProvinceKey).HasMaxLength(64);
            e.Property(x => x.DistrictKey).HasMaxLength(64);
            e.Property(x => x.PaymentMethod).HasMaxLength(32).IsRequired();
            e.Property(x => x.SubtotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasOne(x => x.CustomerProfile).WithMany().HasForeignKey(x => x.CustomerProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SellerUser).WithMany().HasForeignKey(x => x.SellerUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PriceList).WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SaleChannel).WithMany().HasForeignKey(x => x.SaleChannelId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalesInvoiceItem>(e =>
        {
            e.ToTable("SalesInvoiceItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ProductName).HasMaxLength(512).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.SalesInvoice).WithMany(x => x.Items).HasForeignKey(x => x.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesReturn>(e =>
        {
            e.ToTable("SalesReturns");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.CustomerCode).HasMaxLength(64);
            e.Property(x => x.CustomerName).HasMaxLength(256);
            e.Property(x => x.Note).HasColumnType("longtext");
            e.Property(x => x.ReturnType).HasMaxLength(32).IsRequired();
            e.Property(x => x.Status).HasMaxLength(32).IsRequired();
            e.Property(x => x.OtherCollectionType).HasMaxLength(128);
            e.Property(x => x.ReturnSubtotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ReturnDiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ReturnFeeAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.RefundDueAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ExchangeSubtotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.ExchangeDiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.PurchaseDueAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.NetAmountDueFromCustomer).HasColumnType("decimal(18,2)");
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasOne(x => x.CustomerProfile).WithMany().HasForeignKey(x => x.CustomerProfileId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ReceivedByUser).WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SellerUser).WithMany().HasForeignKey(x => x.SellerUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SaleChannel).WithMany().HasForeignKey(x => x.SaleChannelId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SalesReturnItem>(e =>
        {
            e.ToTable("SalesReturnItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ProductName).HasMaxLength(512).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.SalesReturn).WithMany(x => x.ReturnItems).HasForeignKey(x => x.SalesReturnId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SalesReturnExchangeItem>(e =>
        {
            e.ToTable("SalesReturnExchangeItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ProductName).HasMaxLength(512).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.SalesReturn).WithMany(x => x.ExchangeItems).HasForeignKey(x => x.SalesReturnId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentCategory>(e =>
        {
            e.ToTable("PaymentCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(512).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<CashbookParty>(e =>
        {
            e.ToTable("CashbookParties");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(32);
            e.Property(x => x.Address).HasMaxLength(512);
            e.Property(x => x.Province).HasMaxLength(128);
            e.Property(x => x.Ward).HasMaxLength(128);
            e.Property(x => x.Note).HasColumnType("longtext");
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CashbookEntry>(e =>
        {
            e.ToTable("CashbookEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.EntryType).HasMaxLength(16).IsRequired();
            e.Property(x => x.FundType).HasMaxLength(16).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Note).HasColumnType("longtext");
            e.Property(x => x.Status).HasMaxLength(16).IsRequired();
            e.Property(x => x.CounterpartyScope).HasMaxLength(32).IsRequired();
            e.Property(x => x.CounterpartyDisplayName).HasMaxLength(256);
            e.Property(x => x.PartnerDebtMode).HasMaxLength(32).IsRequired();
            e.Property(x => x.SourceKind).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.OccurredAtUtc);
            e.HasIndex(x => new { x.SourceKind, x.SourceId }).IsUnique()
                .HasFilter("`SourceId` IS NOT NULL");
            e.HasOne(x => x.PaymentCategory).WithMany().HasForeignKey(x => x.PaymentCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CashbookParty).WithMany().HasForeignKey(x => x.CashbookPartyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CollectorUser).WithMany().HasForeignKey(x => x.CollectorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.StaffUser).WithMany().HasForeignKey(x => x.StaffUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
