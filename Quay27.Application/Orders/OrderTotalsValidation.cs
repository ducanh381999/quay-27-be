using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Common;

namespace Quay27.Application.Orders;

internal static class OrderTotalsValidation
{
    /// <summary>
    /// Kiểm tra FE gửi tổng tiền hàng, giảm giá, và tổng sau giảm trùng với máy chủ (tính lại).
    /// </summary>
    public static void ValidateSubtotalDiscountTotal(
        decimal serverSubtotal,
        decimal clientSubtotal,
        decimal clientDiscountAmount,
        decimal clientTotal)
    {
        if (!MoneyMath.EqualsMoney(serverSubtotal, clientSubtotal))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("clientSubtotal", "Tổng tiền hàng không khớp, vui lòng tải lại và thử lại."),
            });
        }

        var serverDiscount = MoneyMath.Round(clientDiscountAmount);
        if (!MoneyMath.EqualsMoney(serverDiscount, clientDiscountAmount))
        {
            throw new ValidationException(new[]
                { new ValidationFailure("clientDiscountAmount", "Giảm giá không hợp lệ."), });
        }

        var serverGrand = MoneyMath.Round(serverSubtotal - serverDiscount);
        if (!MoneyMath.EqualsMoney(serverGrand, clientTotal))
        {
            throw new ValidationException(new[]
                { new ValidationFailure("clientTotal", "Khách cần trả không khớp (tổng sau giảm giá)."), });
        }
    }
}
