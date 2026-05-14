using Microsoft.EntityFrameworkCore;
using Quay27.Application.Abstractions;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;
using Quay27.Infrastructure.Persistence;
using Quay27.Infrastructure.Repositories;

namespace Quay27.Products.Tests;

public sealed class CustomerProfileReceivableServiceTests
{
    private sealed class FakeUser : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();
        public string Username => "tester";
        public bool IsAuthenticated => true;
        public IReadOnlyList<string> Roles => Array.Empty<string>();
        public bool IsAdmin => true;
    }

    [Fact]
    public async Task GetDisplayDebt_uses_manual_when_set()
    {
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var id = Guid.NewGuid();
        db.CustomerProfiles.Add(new CustomerProfile
        {
            Id = id,
            CustomerCode = "KH001",
            CustomerName = "A",
            Phone1 = "1",
            Phone2 = "",
            Birthday = null,
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
            ManualCurrentDebt = 42m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "t",
            IsActive = true,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();
        var repo = new CustomerProfileRepository(db);
        var d = await repo.GetDisplayDebtAsync(id, default);
        Assert.Equal(42m, d);
    }
}
