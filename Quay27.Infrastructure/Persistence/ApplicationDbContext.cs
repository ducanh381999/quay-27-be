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
    }
}
