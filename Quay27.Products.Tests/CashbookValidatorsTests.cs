using Quay27.Application.Cashbook;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class CashbookValidatorsTests
{
    [Fact]
    public void CreateCashbookReceiptRequestValidator_rejects_zero_amount()
    {
        var v = new CreateCashbookReceiptRequestValidator();
        var r = v.Validate(new CreateCashbookReceiptRequest(
            OccurredAtUtc: null,
            PaymentCategoryId: 11,
            CollectorUserId: Guid.NewGuid(),
            CounterpartyScope: "other",
            CashbookPartyId: null,
            CounterpartyDisplayName: "A",
            Amount: 0,
            Note: null,
            AffectsBusinessResult: false,
            FundType: "cash",
            PartnerDebtMode: "not_applicable",
            StaffUserId: null));
        Assert.False(r.IsValid);
    }

    [Fact]
    public void CreateCashbookReceiptRequestValidator_accepts_valid()
    {
        var v = new CreateCashbookReceiptRequestValidator();
        var r = v.Validate(new CreateCashbookReceiptRequest(
            OccurredAtUtc: null,
            PaymentCategoryId: 11,
            CollectorUserId: Guid.NewGuid(),
            CounterpartyScope: "other",
            CashbookPartyId: null,
            CounterpartyDisplayName: "A",
            Amount: 100,
            Note: null,
            AffectsBusinessResult: true,
            FundType: "cash",
            PartnerDebtMode: "not_applicable",
            StaffUserId: null));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void CreateCashbookPaymentRequestValidator_rejects_invalid_fund()
    {
        var v = new CreateCashbookPaymentRequestValidator();
        var r = v.Validate(new CreateCashbookPaymentRequest(
            OccurredAtUtc: null,
            PaymentCategoryId: 8,
            CollectorUserId: Guid.NewGuid(),
            CounterpartyScope: "other",
            CashbookPartyId: null,
            CounterpartyDisplayName: "B",
            Amount: 50,
            Note: null,
            AffectsBusinessResult: false,
            FundType: "invalid",
            PartnerDebtMode: "not_applicable",
            StaffUserId: null));
        Assert.False(r.IsValid);
    }
}
