using Microsoft.EntityFrameworkCore;
using Quay27.Application.Dashboard;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;
using Quay27.Infrastructure.Services;

namespace Quay27.Products.Tests;

public class DashboardServiceGetTopProductsTests
{
    [Fact]
    public async Task GetTopProductsAsync_this_month_net_revenue_materializes_without_translation_error()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var custId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);
        db.Customers.Add(new Customer
        {
            Id = custId,
            SortOrder = 1,
            InvoiceCode = "INV1",
            BillCreatedAt = utcNow,
            NameAddress = "A",
            Notes = "",
            Status = "",
            SheetDate = new DateOnly(2026, 5, 12),
            CreatedDate = utcNow,
            CreatedBy = "t",
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        db.CustomerInvoiceLines.Add(new CustomerInvoiceLine
        {
            Id = Guid.NewGuid(),
            CustomerId = custId,
            ProductNameSnapshot = "Widget",
            Quantity = 2,
            Amount = 100m,
        });
        await db.SaveChangesAsync();

        var svc = new DashboardService(db);
        var result = await svc.GetTopProductsAsync(
            DashboardPreset.ThisMonth,
            DashboardProductMetric.NetRevenue,
            utcNow);

        var row = Assert.Single(result);
        Assert.Equal("Widget", row.Name);
        Assert.Equal(100m, row.Value);
    }
}
