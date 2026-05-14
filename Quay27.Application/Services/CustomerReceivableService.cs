using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.CustomerProfiles;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class CustomerReceivableService : ICustomerReceivableService
{
    private readonly ICustomerProfileRepository _profiles;
    private readonly ICustomerReceivableTransactionRepository _transactions;
    private readonly IReceivingAccountRepository _receivingAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RecordCustomerPaymentRequest> _paymentValidator;
    private readonly IValidator<RecordCustomerAdjustmentRequest> _adjustmentValidator;
    private readonly IValidator<RecordCustomerPaymentDiscountRequest> _discountValidator;
    private readonly IValidator<RecordCustomerQrPaymentRequest> _qrValidator;

    public CustomerReceivableService(
        ICustomerProfileRepository profiles,
        ICustomerReceivableTransactionRepository transactions,
        IReceivingAccountRepository receivingAccounts,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IValidator<RecordCustomerPaymentRequest> paymentValidator,
        IValidator<RecordCustomerAdjustmentRequest> adjustmentValidator,
        IValidator<RecordCustomerPaymentDiscountRequest> discountValidator,
        IValidator<RecordCustomerQrPaymentRequest> qrValidator)
    {
        _profiles = profiles;
        _transactions = transactions;
        _receivingAccounts = receivingAccounts;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _paymentValidator = paymentValidator;
        _adjustmentValidator = adjustmentValidator;
        _discountValidator = discountValidator;
        _qrValidator = qrValidator;
    }

    public async Task<PagedReceivableTransactionsResult> ListTransactionsAsync(Guid customerProfileId, int skip,
        int take, string? kind, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (await _profiles.GetByIdAsync(customerProfileId, cancellationToken) is null)
            throw new NotFoundException("Customer profile not found.");
        var (items, total) =
            await _transactions.ListPagedAsync(customerProfileId, skip, take, kind, cancellationToken);
        return new PagedReceivableTransactionsResult(items.Select(Map).ToList(), total);
    }

    public async Task<CustomerReceivableTransactionDto> RecordPaymentAsync(Guid customerProfileId,
        RecordCustomerPaymentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _paymentValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            await ApplyDebtReductionAsync(
                customerProfileId,
                CustomerReceivableTransactionKinds.Payment,
                ToUtc(request.OccurredAtUtc),
                request.Amount,
                request.CollectorName.Trim(),
                request.PaymentMethod.Trim(),
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                null,
                request.AllocateToInvoice,
                null,
                cancellationToken), cancellationToken);
    }

    public async Task<CustomerReceivableTransactionDto> RecordPaymentDiscountAsync(Guid customerProfileId,
        RecordCustomerPaymentDiscountRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _discountValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            await ApplyDebtReductionAsync(
                customerProfileId,
                CustomerReceivableTransactionKinds.PaymentDiscount,
                ToUtc(request.OccurredAtUtc),
                request.DiscountAmount,
                request.PerformerName.Trim(),
                null,
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                null,
                request.AllocateToInvoice,
                null,
                cancellationToken), cancellationToken);
    }

    public async Task<CustomerReceivableTransactionDto> RecordQrPaymentAsync(Guid customerProfileId,
        RecordCustomerQrPaymentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _qrValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            await ApplyDebtReductionAsync(
                customerProfileId,
                CustomerReceivableTransactionKinds.QrPayment,
                DateTime.UtcNow,
                request.Amount,
                _currentUser.Username,
                "qr_transfer",
                string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                request.ReceivingAccountId,
                false,
                null,
                cancellationToken), cancellationToken);
    }

    public async Task<CustomerReceivableTransactionDto> RecordAdjustmentAsync(Guid customerProfileId,
        RecordCustomerAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _adjustmentValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var profile = await _profiles.GetTrackedByIdAsync(customerProfileId, cancellationToken);
            if (profile is null)
                throw new NotFoundException("Customer profile not found.");

            var current = await _profiles.GetDisplayDebtAsync(customerProfileId, cancellationToken);
            var newBalance = Math.Max(0m, request.NewDebtAbsolute);
            var delta = newBalance - current;

            profile.ManualCurrentDebt = newBalance;
            profile.UpdatedBy = _currentUser.Username;
            profile.UpdatedDate = DateTime.UtcNow;

            var tx = new CustomerReceivableTransaction
            {
                Id = Guid.NewGuid(),
                CustomerProfileId = customerProfileId,
                Kind = CustomerReceivableTransactionKinds.Adjustment,
                OccurredAtUtc = ToUtc(request.OccurredAtUtc),
                Amount = delta,
                BalanceAfter = newBalance,
                PaymentMethod = null,
                CollectorOrPerformerName = null,
                Note = null,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                ReceivingAccountId = null,
                AllocateToInvoice = false,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUser.Username,
            };
            await _transactions.AddAsync(tx, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(tx);
        }, cancellationToken);
    }

    private async Task<CustomerReceivableTransactionDto> ApplyDebtReductionAsync(
        Guid customerProfileId,
        string kind,
        DateTime occurredAtUtc,
        decimal amount,
        string collectorOrPerformer,
        string? paymentMethod,
        string? note,
        Guid? receivingAccountId,
        bool allocateToInvoice,
        string? description,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetTrackedByIdAsync(customerProfileId, cancellationToken);
        if (profile is null)
            throw new NotFoundException("Customer profile not found.");

        var current = await _profiles.GetDisplayDebtAsync(customerProfileId, cancellationToken);
        if (amount <= 0)
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(amount), "Số tiền phải lớn hơn 0.") });
        if (amount > current)
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(amount), "Số tiền không được vượt nợ hiện tại.") });

        if (receivingAccountId.HasValue &&
            await _receivingAccounts.GetByIdAsync(receivingAccountId.Value, cancellationToken) is null)
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(receivingAccountId), "Tài khoản thu không hợp lệ.") });

        var newBalance = Math.Max(0m, current - amount);
        profile.ManualCurrentDebt = newBalance;
        profile.UpdatedBy = _currentUser.Username;
        profile.UpdatedDate = DateTime.UtcNow;

        var tx = new CustomerReceivableTransaction
        {
            Id = Guid.NewGuid(),
            CustomerProfileId = customerProfileId,
            Kind = kind,
            OccurredAtUtc = occurredAtUtc,
            Amount = amount,
            BalanceAfter = newBalance,
            PaymentMethod = paymentMethod,
            CollectorOrPerformerName = collectorOrPerformer,
            Note = note,
            Description = description,
            ReceivingAccountId = receivingAccountId,
            AllocateToInvoice = allocateToInvoice,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = _currentUser.Username,
        };
        await _transactions.AddAsync(tx, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(tx);
    }

    private static DateTime ToUtc(DateTime v) =>
        v.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(v, DateTimeKind.Utc)
            : v.ToUniversalTime();

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }

    private static CustomerReceivableTransactionDto Map(CustomerReceivableTransaction x) =>
        new(
            x.Id,
            x.Kind,
            x.OccurredAtUtc,
            x.Amount,
            x.BalanceAfter,
            x.PaymentMethod,
            x.CollectorOrPerformerName,
            x.Note,
            x.Description,
            x.ReceivingAccountId,
            x.AllocateToInvoice,
            x.CreatedAtUtc,
            x.CreatedBy);
}
