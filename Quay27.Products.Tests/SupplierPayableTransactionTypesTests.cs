using Quay27.Application.Suppliers;

namespace Quay27.Products.Tests;

public class SupplierPayableTransactionTypesTests
{
    [Fact]
    public void GetLabel_returns_vietnamese_labels()
    {
        Assert.Equal("Thanh toán", SupplierPayableTransactionTypes.GetLabel(SupplierPayableTransactionTypes.Payment));
        Assert.Equal("Nhập hàng", SupplierPayableTransactionTypes.GetLabel(SupplierPayableTransactionTypes.GoodsReceipt));
        Assert.Equal("Chiết khấu thanh toán",
            SupplierPayableTransactionTypes.GetLabel(SupplierPayableTransactionTypes.PaymentDiscount));
    }

    [Fact]
    public void AllFilterOptions_has_unique_values()
    {
        var opts = SupplierPayableTransactionTypes.AllFilterOptions();
        var set = new HashSet<int>();
        foreach (var (v, _) in opts)
        {
            Assert.True(set.Add(v), $"Duplicate type {v}");
        }
    }
}
