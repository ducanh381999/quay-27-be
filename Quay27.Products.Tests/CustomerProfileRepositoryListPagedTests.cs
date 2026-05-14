using Microsoft.EntityFrameworkCore;
using Quay27.Application.CustomerProfiles;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;
using Quay27.Infrastructure.Repositories;

namespace Quay27.Products.Tests;

public class CustomerProfileRepositoryListPagedTests
{
    private static DbContextOptions<ApplicationDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static CustomerProfile NewProfile(Guid id, string code = "P001") => new()
    {
        Id = id,
        CustomerCode = code,
        CustomerName = "Test Customer",
        Phone1 = "",
        Phone2 = "",
        Gender = "",
        Email = "",
        Facebook = "",
        Address = "",
        ProvinceCity = "",
        Ward = "",
        CustomerGroup = "",
        Note = "",
        BuyerType = "individual",
        BuyerName = "",
        TaxCode = "",
        InvoiceAddress = "",
        InvoiceProvinceCity = "",
        InvoiceWard = "",
        IdentityNumber = "",
        PassportNumber = "",
        InvoiceEmail = "",
        InvoicePhone = "",
        BankName = "",
        BankAccountNumber = "",
        RewardPointsBalance = 0,
        RewardPointsLifetime = 0,
        CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CreatedBy = "test",
        IsDeleted = false,
        IsActive = true,
    };

    [Fact]
    public async Task ListPagedAsync_empty_database_returns_zero_total()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();
        var repo = new CustomerProfileRepository(db);

        var (_, total) = await repo.ListPagedAsync(new CustomerProfileListQuery());

        Assert.Equal(0, total);
    }

    [Fact]
    public async Task ListPagedAsync_profile_without_invoices_materializes_zero_aggregates()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();
        var pid = Guid.NewGuid();
        db.CustomerProfiles.Add(NewProfile(pid));
        await db.SaveChangesAsync();
        var repo = new CustomerProfileRepository(db);

        var (items, total) = await repo.ListPagedAsync(new CustomerProfileListQuery { Take = 10, Skip = 0 });

        Assert.Equal(1, total);
        var row = Assert.Single(items);
        Assert.Equal(pid, row.Profile.Id);
        Assert.Equal(0m, row.TotalPaid);
        Assert.Equal(0m, row.InvoiceDebt);
        Assert.Equal(0m, row.ReturnTotal);
    }

    [Fact]
    public async Task ListPagedAsync_sums_invoices_and_returns_for_profile()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();
        var pid = Guid.NewGuid();
        db.CustomerProfiles.Add(NewProfile(pid));
        db.SalesInvoices.Add(new SalesInvoice
        {
            Id = Guid.NewGuid(),
            Code = "INV-1",
            CreatedAtUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc),
            CustomerProfileId = pid,
            Status = "completed",
            SubtotalAmount = 100m,
            DiscountAmount = 10m,
            PaidAmount = 30m,
        });
        await db.SaveChangesAsync();
        var repo = new CustomerProfileRepository(db);

        var (items, total) = await repo.ListPagedAsync(new CustomerProfileListQuery { Take = 10, Skip = 0 });

        Assert.Equal(1, total);
        var row = Assert.Single(items);
        Assert.Equal(30m, row.TotalPaid);
        Assert.Equal(60m, row.InvoiceDebt);
    }

    [Fact]
    public async Task ListPagedAsync_mixed_profiles_with_and_without_invoices_all_materialize()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();
        var noInvId = Guid.NewGuid();
        var withInvId = Guid.NewGuid();
        db.CustomerProfiles.Add(NewProfile(noInvId, "NOINV"));
        db.CustomerProfiles.Add(NewProfile(withInvId, "HASINV"));
        db.SalesInvoices.Add(new SalesInvoice
        {
            Id = Guid.NewGuid(),
            Code = "INV-2",
            CreatedAtUtc = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            CustomerProfileId = withInvId,
            Status = "completed",
            SubtotalAmount = 50m,
            DiscountAmount = 0m,
            PaidAmount = 50m,
        });
        await db.SaveChangesAsync();
        var repo = new CustomerProfileRepository(db);

        var (items, total) = await repo.ListPagedAsync(new CustomerProfileListQuery { Take = 20, Skip = 0 });

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        var noInv = items.Single(x => x.Profile.Id == noInvId);
        Assert.Equal(0m, noInv.TotalPaid);
        Assert.Equal(0m, noInv.InvoiceDebt);
        var withInv = items.Single(x => x.Profile.Id == withInvId);
        Assert.Equal(50m, withInv.TotalPaid);
        Assert.Equal(0m, withInv.InvoiceDebt);
    }
}
