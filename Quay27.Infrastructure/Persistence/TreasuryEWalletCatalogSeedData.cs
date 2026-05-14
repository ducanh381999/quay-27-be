using Quay27.Domain.Entities;

namespace Quay27.Infrastructure.Persistence;

internal static class TreasuryEWalletCatalogSeedData
{
    public static IReadOnlyList<EWalletCatalogItem> BuildItems() =>
        new List<EWalletCatalogItem>
        {
            new()
            {
                Code = "MoMo",
                FullName = "MoMo",
                GlobalName = "MoMo",
                OrderNum = 1,
                SearchText = "momo momo",
                IsActive = true,
                CountryId = 1,
                Type = 1,
            },
            new()
            {
                Code = "ZaloPay",
                FullName = "Zalopay",
                GlobalName = "ZaloPay",
                OrderNum = 2,
                SearchText = "zalopay zalopay",
                IsActive = true,
                CountryId = 1,
                Type = 1,
            },
            new()
            {
                Code = "ViettelPay",
                FullName = "ViettelPay",
                GlobalName = "ViettelPay",
                OrderNum = 3,
                SearchText = "viettelpay viettelpay",
                IsActive = true,
                CountryId = 1,
                Type = 1,
            },
            new()
            {
                Code = "ShopeePay",
                FullName = "ShopeePay",
                GlobalName = "ShopeePay",
                OrderNum = 4,
                SearchText = "shopeepay shopeepay",
                IsActive = true,
                CountryId = 1,
                Type = 1,
            },
            new()
            {
                Code = "VNPAY",
                FullName = "VNPAY",
                GlobalName = "VNPAY",
                OrderNum = 5,
                SearchText = "vnpay vnpay",
                IsActive = true,
                CountryId = 1,
                Type = 1,
            },
        };
}
