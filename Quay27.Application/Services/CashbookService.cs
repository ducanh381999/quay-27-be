using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;
using Quay27.Application.Common;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class CashbookService : ICashbookService
{
    private readonly ICashbookRepository _cashbook;
    private readonly IPaymentCategoryRepository _categories;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCashbookReceiptRequest> _receiptValidator;
    private readonly IValidator<CreateCashbookPaymentRequest> _paymentValidator;
    private readonly IValidator<CreateCashbookPartyRequest> _partyValidator;

    public CashbookService(
        ICashbookRepository cashbook,
        IPaymentCategoryRepository categories,
        IUserRepository users,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreateCashbookReceiptRequest> receiptValidator,
        IValidator<CreateCashbookPaymentRequest> paymentValidator,
        IValidator<CreateCashbookPartyRequest> partyValidator)
    {
        _cashbook = cashbook;
        _categories = categories;
        _users = users;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _receiptValidator = receiptValidator;
        _paymentValidator = paymentValidator;
        _partyValidator = partyValidator;
    }

    public Task<IReadOnlyList<CashbookEntryListItemDto>> ListEntriesAsync(CashbookListQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _cashbook.ListEntriesAsync(query, cancellationToken);
    }

    public Task<CashbookSummaryDto> GetSummaryAsync(CashbookListQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _cashbook.GetSummaryAsync(
            query.FromUtc,
            query.ToUtc,
            query.FundType,
            query.EntryTypes,
            query.PaymentCategoryId,
            query.Statuses,
            query.AffectsBusinessResult,
            query.CreatedByUserId,
            query.StaffUserId,
            query.SearchCode,
            query.CounterpartySearch,
            query.CounterpartyPhone,
            query.PartnerDebtModes,
            cancellationToken);
    }

    public async Task<CashbookPartyCreatedDto> CreatePartyAsync(CreateCashbookPartyRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var vr = await _partyValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        var id = Guid.NewGuid();
        var entity = new CashbookParty
        {
            Id = id,
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            Province = request.Province?.Trim(),
            Ward = request.Ward?.Trim(),
            Note = request.Note?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _cashbook.AddPartyAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new CashbookPartyCreatedDto(id, entity.Name);
    }

    public async Task<CashbookEntryCreatedDto> CreateReceiptAsync(CreateCashbookReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var vr = await _receiptValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        await EnsureCollectorExistsAsync(request.CollectorUserId, cancellationToken);

        var category = await _categories.GetByIdAsync(request.PaymentCategoryId, cancellationToken)
                       ?? throw new ValidationException(new[]
                       {
                           new ValidationFailure(nameof(request.PaymentCategoryId), "Loại thu không tồn tại."),
                       });

        if (!string.Equals(category.Kind, "Receipt", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.PaymentCategoryId), "Danh mục không phải loại thu."),
            });
        }

        var (displayName, partyId) = await ResolveCounterpartyAsync(request, cancellationToken);

        var code = await _cashbook.GenerateNextReceiptCodeAsync(category.Code, cancellationToken);
        var entry = new CashbookEntry
        {
            Id = Guid.NewGuid(),
            Code = code,
            EntryType = "Receipt",
            FundType = request.FundType.Trim().ToLowerInvariant(),
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
            Amount = MoneyMath.Round(request.Amount),
            Note = request.Note?.Trim(),
            PaymentCategoryId = category.Id,
            AffectsBusinessResult = request.AffectsBusinessResult,
            Status = "paid",
            CollectorUserId = request.CollectorUserId,
            CounterpartyScope = request.CounterpartyScope.Trim().ToLowerInvariant(),
            CashbookPartyId = partyId,
            CounterpartyDisplayName = displayName,
            PartnerDebtMode = request.PartnerDebtMode.Trim().ToLowerInvariant(),
            SourceKind = "Manual",
            SourceId = null,
            CreatedByUserId = _currentUser.UserId,
            StaffUserId = request.StaffUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _cashbook.AddEntryAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new CashbookEntryCreatedDto(entry.Id, entry.Code);
    }

    public async Task<CashbookEntryCreatedDto> CreatePaymentAsync(CreateCashbookPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var vr = await _paymentValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        await EnsureCollectorExistsAsync(request.CollectorUserId, cancellationToken);

        var category = await _categories.GetByIdAsync(request.PaymentCategoryId, cancellationToken)
                       ?? throw new ValidationException(new[]
                       {
                           new ValidationFailure(nameof(request.PaymentCategoryId), "Loại chi không tồn tại."),
                       });

        if (!string.Equals(category.Kind, "Expense", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.PaymentCategoryId), "Danh mục không phải loại chi."),
            });
        }

        var (displayName, partyId) = await ResolveCounterpartyAsync(request, cancellationToken);

        var code = await _cashbook.GenerateNextPaymentCodeAsync(cancellationToken);
        var entry = new CashbookEntry
        {
            Id = Guid.NewGuid(),
            Code = code,
            EntryType = "Payment",
            FundType = request.FundType.Trim().ToLowerInvariant(),
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
            Amount = MoneyMath.Round(request.Amount),
            Note = request.Note?.Trim(),
            PaymentCategoryId = category.Id,
            AffectsBusinessResult = request.AffectsBusinessResult,
            Status = "paid",
            CollectorUserId = request.CollectorUserId,
            CounterpartyScope = request.CounterpartyScope.Trim().ToLowerInvariant(),
            CashbookPartyId = partyId,
            CounterpartyDisplayName = displayName,
            PartnerDebtMode = request.PartnerDebtMode.Trim().ToLowerInvariant(),
            SourceKind = "Manual",
            SourceId = null,
            CreatedByUserId = _currentUser.UserId,
            StaffUserId = request.StaffUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _cashbook.AddEntryAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new CashbookEntryCreatedDto(entry.Id, entry.Code);
    }

    private async Task EnsureCollectorExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("CollectorUserId", "Người thu/chi không tồn tại."),
            });
        }
    }

    private async Task<(string? DisplayName, Guid? PartyId)> ResolveCounterpartyAsync(
        CreateCashbookReceiptRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CashbookPartyId.HasValue)
        {
            var party = await _cashbook.GetPartyByIdAsync(request.CashbookPartyId.Value, cancellationToken)
                        ?? throw new ValidationException(new[]
                        {
                            new ValidationFailure(nameof(request.CashbookPartyId), "Người nộp/nhận không tồn tại."),
                        });

            var name = string.IsNullOrWhiteSpace(request.CounterpartyDisplayName)
                ? party.Name
                : request.CounterpartyDisplayName.Trim();
            return (name, party.Id);
        }

        if (string.IsNullOrWhiteSpace(request.CounterpartyDisplayName))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.CounterpartyDisplayName), "Nhập tên người nộp/nhận."),
            });
        }

        return (request.CounterpartyDisplayName.Trim(), null);
    }

    private async Task<(string? DisplayName, Guid? PartyId)> ResolveCounterpartyAsync(
        CreateCashbookPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CashbookPartyId.HasValue)
        {
            var party = await _cashbook.GetPartyByIdAsync(request.CashbookPartyId.Value, cancellationToken)
                        ?? throw new ValidationException(new[]
                        {
                            new ValidationFailure(nameof(request.CashbookPartyId), "Người nộp/nhận không tồn tại."),
                        });

            var name = string.IsNullOrWhiteSpace(request.CounterpartyDisplayName)
                ? party.Name
                : request.CounterpartyDisplayName.Trim();
            return (name, party.Id);
        }

        if (string.IsNullOrWhiteSpace(request.CounterpartyDisplayName))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.CounterpartyDisplayName), "Nhập tên người nộp/nhận."),
            });
        }

        return (request.CounterpartyDisplayName.Trim(), null);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
