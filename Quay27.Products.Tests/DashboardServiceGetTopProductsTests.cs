using Microsoft.EntityFrameworkCore;
using Quay27.Application.Dashboard;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;
using Quay27.Infrastructure.Services;

namespace Quay27.Products.Tests;

public class DashboardServiceGetTopProductsTests
{
    private static DbContextOptions<ApplicationDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task GetTopProductsAsync_this_month_net_revenue_aggregates_invoice_lines_by_product()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();

        var productId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);
        var invoiceId = Guid.NewGuid();

        db.Products.Add(new Product
        {
            Id = productId,
            Code = "A01",
            Name = "Widget",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.SalesInvoices.Add(new SalesInvoice
        {
            Id = invoiceId,
            Code = "HD000001",
            CreatedAtUtc = utcNow,
            Status = "completed",
            SubtotalAmount = 150m,
            DiscountAmount = 0m,
            PaidAmount = 150m,
        });
        db.SalesInvoiceItems.AddRange(
            new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = invoiceId,
                ProductId = productId,
                ProductCode = "A01",
                ProductName = "Widget",
                Quantity = 1,
                UnitPrice = 50m,
                LineTotal = 50m,
            },
            new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = invoiceId,
                ProductId = productId,
                ProductCode = "A01",
                ProductName = "Widget",
                Quantity = 2,
                UnitPrice = 50m,
                LineTotal = 100m,
            });
        await db.SaveChangesAsync();

        var svc = new DashboardService(db);
        var result = await svc.GetTopProductsAsync(
            DashboardPreset.ThisMonth,
            DashboardProductMetric.NetRevenue,
            utcNow);

        var row = Assert.Single(result);
        Assert.Equal("Widget", row.Name);
        Assert.Equal(150m, row.Value);
    }

    [Fact]
    public async Task GetTopProductsAsync_excludes_cancelled_invoices()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();

        var productId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);
        var activeInvoiceId = Guid.NewGuid();
        var cancelledInvoiceId = Guid.NewGuid();

        db.Products.Add(new Product
        {
            Id = productId,
            Code = "A01",
            Name = "Widget",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.SalesInvoices.AddRange(
            new SalesInvoice
            {
                Id = activeInvoiceId,
                Code = "HD000001",
                CreatedAtUtc = utcNow,
                Status = "completed",
                SubtotalAmount = 100m,
                DiscountAmount = 0m,
                PaidAmount = 100m,
            },
            new SalesInvoice
            {
                Id = cancelledInvoiceId,
                Code = "HD000002",
                CreatedAtUtc = utcNow,
                Status = "cancelled",
                SubtotalAmount = 500m,
                DiscountAmount = 0m,
                PaidAmount = 0m,
            });
        db.SalesInvoiceItems.AddRange(
            new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = activeInvoiceId,
                ProductId = productId,
                ProductCode = "A01",
                ProductName = "Widget",
                Quantity = 1,
                UnitPrice = 100m,
                LineTotal = 100m,
            },
            new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = cancelledInvoiceId,
                ProductId = productId,
                ProductCode = "A01",
                ProductName = "Widget",
                Quantity = 10,
                UnitPrice = 50m,
                LineTotal = 500m,
            });
        await db.SaveChangesAsync();

        var svc = new DashboardService(db);
        var result = await svc.GetTopProductsAsync(
            DashboardPreset.ThisMonth,
            DashboardProductMetric.NetRevenue,
            utcNow);

        var row = Assert.Single(result);
        Assert.Equal(100m, row.Value);
    }

    [Fact]
    public async Task GetTopProductsAsync_quantity_metric_sums_line_quantities()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        await db.Database.EnsureCreatedAsync();

        var productId = Guid.NewGuid();
        var utcNow = new DateTime(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);
        var invoiceId = Guid.NewGuid();

        db.Products.Add(new Product
        {
            Id = productId,
            Code = "A01",
            Name = "Widget",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.SalesInvoices.Add(new SalesInvoice
        {
            Id = invoiceId,
            Code = "HD000001",
            CreatedAtUtc = utcNow,
            Status = "processing",
            SubtotalAmount = 150m,
            DiscountAmount = 0m,
            PaidAmount = 0m,
        });
        db.SalesInvoiceItems.AddRange(
            new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = invoiceId,
                ProductId = productId,
                ProductCode = "A01",
                ProductName = "Widget",
                Quantity = 2,
                UnitPrice = 50m,
                LineTotal = 100m,
            },
            new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = invoiceId,
                ProductId = productId,
                ProductCode = "A01",
                ProductName = "Widget",
                Quantity = 3,
                UnitPrice = 50m,
                LineTotal = 50m,
            });
        await db.SaveChangesAsync();

        var svc = new DashboardService(db);
        var result = await svc.GetTopProductsAsync(
            DashboardPreset.ThisMonth,
            DashboardProductMetric.Quantity,
            utcNow);

        var row = Assert.Single(result);
        Assert.Equal(5m, row.Value);
    }
}
