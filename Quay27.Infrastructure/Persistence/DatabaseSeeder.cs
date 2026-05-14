using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quay27.Domain.Constants;
using Quay27.Domain.Entities;

namespace Quay27.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        await EnsureSaleChannelsAsync(db, logger, cancellationToken);

        await EnsureTreasuryCatalogsAsync(db, logger, cancellationToken);

        if (await db.Roles.AnyAsync(cancellationToken))
            return;

        logger.LogInformation("Seeding initial roles, queue, and admin user.");

        db.Roles.AddRange(
            new Role { Id = 1, Name = SchemaConstants.Roles.Admin },
            new Role { Id = 2, Name = SchemaConstants.Roles.Staff });

        db.Queues.Add(new Queue { Id = 1, Name = "Quầy 27", IsActive = true });

        var adminPassword = configuration["Seed:AdminPassword"] ?? "ChangeMe!123";
        var admin = new User
        {
            Id = AdminUserId,
            Username = "admin",
            FullName = "Administrator",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            PasswordHash = string.Empty
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);
        db.Users.Add(admin);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = 1 });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static readonly Guid SaleChannelDirectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid SaleChannelOtherId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

    private static async Task EnsureSaleChannelsAsync(ApplicationDbContext db, ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await db.SaleChannels.AnyAsync(cancellationToken))
            return;

        logger.LogInformation("Seeding default sale channels.");
        var utc = DateTime.UtcNow;
        db.SaleChannels.AddRange(
            new SaleChannel
            {
                Id = SaleChannelDirectId,
                Name = "Bán trực tiếp",
                Description = null,
                IsActive = true,
                CreatedAtUtc = utc
            },
            new SaleChannel
            {
                Id = SaleChannelOtherId,
                Name = "Khác",
                Description = null,
                IsActive = true,
                CreatedAtUtc = utc
            });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureTreasuryCatalogsAsync(ApplicationDbContext db, ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await db.BankCatalogItems.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seeding bank catalog metadata.");
            db.BankCatalogItems.AddRange(TreasuryBankCatalogSeedData.BuildItems());
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.EWalletCatalogItems.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seeding e-wallet catalog metadata.");
            db.EWalletCatalogItems.AddRange(TreasuryEWalletCatalogSeedData.BuildItems());
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
