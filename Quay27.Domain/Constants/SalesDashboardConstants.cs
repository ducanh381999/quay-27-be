using Quay27.Domain.Entities;

namespace Quay27.Domain.Constants;

/// <summary>
/// Quy tắc nhận diện đơn trả hàng (bán) cho dashboard. Điền thêm marker khi nghiệp vụ thống nhất.
/// </summary>
public static class SalesDashboardConstants
{
    /// <summary>So khớp không phân biệt hoa thường với <see cref="Customer.Status"/>.</summary>
    public static readonly string[] SalesReturnStatusContainsMarkers =
    {
        // Ví dụ: "Trả hàng", "Đơn trả" — bật khi có giá trị Status thực tế trong DB.
    };

    /// <summary>So khớp không phân biệt hoa thường với <see cref="Customer.Notes"/>.</summary>
    public static readonly string[] SalesReturnNotesContainsMarkers =
    {
    };

    public static bool IsSalesReturn(Customer c) => IsSalesReturn(c.Status, c.Notes);

    public static bool IsSalesReturn(string status, string notes)
    {
        foreach (var m in SalesReturnStatusContainsMarkers)
        {
            if (!string.IsNullOrEmpty(m) &&
                status.Contains(m, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var m in SalesReturnNotesContainsMarkers)
        {
            if (!string.IsNullOrEmpty(m) &&
                notes.Contains(m, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
