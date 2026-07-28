using Microsoft.EntityFrameworkCore;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;
using Quay27.Infrastructure.Repositories;

namespace Quay27.Products.Tests;

public class PriceListItemRepositoryTests
{
    private static DbContextOptions<ApplicationDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task ListByPriceListIdsAsync_should_filter_under_stock_limit()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        var priceListId = Guid.NewGuid();
        var under = new Product
        {
            Id = Guid.NewGuid(),
            Code = "SP001",
            Name = "Under",
            Stock = 2,
            MinStock = 5,
            RowStatus = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var normal = new Product
        {
            Id = Guid.NewGuid(),
            Code = "SP002",
            Name = "Normal",
            Stock = 8,
            MinStock = 5,
            RowStatus = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Products.AddRange(under, normal);
        db.PriceListItems.AddRange(
            new PriceListItem { Id = Guid.NewGuid(), PriceListId = priceListId, ProductId = under.Id, Product = under, Price = 1, CreatedAt = DateTimeOffset.UtcNow },
            new PriceListItem { Id = Guid.NewGuid(), PriceListId = priceListId, ProductId = normal.Id, Product = normal, Price = 1, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var repo = new PriceListItemRepository(db);
        var result = await repo.ListByPriceListIdsAsync([priceListId], null, null, "under_stock_limit", null, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(under.Id, item.ProductId);
    }

    [Fact]
    public async Task ListByPriceListIdsAsync_should_filter_over_stock_limit()
    {
        await using var db = new ApplicationDbContext(NewInMemoryOptions());
        var priceListId = Guid.NewGuid();
        var over = new Product
        {
            Id = Guid.NewGuid(),
            Code = "SP001",
            Name = "Over",
            Stock = 12,
            MaxStock = 9,
            RowStatus = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var normal = new Product
        {
            Id = Guid.NewGuid(),
            Code = "SP002",
            Name = "Normal",
            Stock = 7,
            MaxStock = 9,
            RowStatus = "active",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Products.AddRange(over, normal);
        db.PriceListItems.AddRange(
            new PriceListItem { Id = Guid.NewGuid(), PriceListId = priceListId, ProductId = over.Id, Product = over, Price = 1, CreatedAt = DateTimeOffset.UtcNow },
            new PriceListItem { Id = Guid.NewGuid(), PriceListId = priceListId, ProductId = normal.Id, Product = normal, Price = 1, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var repo = new PriceListItemRepository(db);
        var result = await repo.ListByPriceListIdsAsync([priceListId], null, null, "over_stock_limit", null, CancellationToken.None);

        var item = Assert.Single(result);
        Assert.Equal(over.Id, item.ProductId);
    }
}
