using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Repositories;
using Quay27.Domain.Constants;

namespace Quay27.Application.Orders;

public static class OrderReceivingAccountResolver
{
    public static readonly HashSet<string> AllowedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase) { "cash", "transfer", "card", "wallet" };

    public static async Task<(string PaymentMethod, Guid? ReceivingAccountId)> ResolveAsync(
        string? paymentMethodRaw,
        Guid? receivingAccountIdRequest,
        IReceivingAccountRepository receivingAccounts,
        CancellationToken cancellationToken)
    {
        var paymentMethod = (paymentMethodRaw ?? "cash").Trim().ToLowerInvariant();
        if (!AllowedPaymentMethods.Contains(paymentMethod))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("paymentMethod", "Phương thức thanh toán không hợp lệ."),
            });
        }

        Guid? receivingAccountId = null;
        if (paymentMethod is "transfer" or "card")
        {
            if (!receivingAccountIdRequest.HasValue)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure("receivingAccountId",
                        "Chọn tài khoản ngân hàng cho phương thức thanh toán này."),
                });
            }

            var acc = await receivingAccounts.GetByIdAsync(receivingAccountIdRequest.Value, cancellationToken);
            if (acc is null || !string.Equals(acc.AccountKind, TreasuryConstants.AccountKindBank,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure("receivingAccountId", "Tài khoản ngân hàng không hợp lệ.") });
            }

            receivingAccountId = acc.Id;
        }
        else if (paymentMethod == "wallet")
        {
            if (!receivingAccountIdRequest.HasValue)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure("receivingAccountId",
                        "Chọn ví điện tử cho phương thức thanh toán này."),
                });
            }

            var acc = await receivingAccounts.GetByIdAsync(receivingAccountIdRequest.Value, cancellationToken);
            if (acc is null || !string.Equals(acc.AccountKind, TreasuryConstants.AccountKindEWallet,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure("receivingAccountId", "Ví điện tử không hợp lệ.") });
            }

            receivingAccountId = acc.Id;
        }

        return (paymentMethod, receivingAccountId);
    }
}
