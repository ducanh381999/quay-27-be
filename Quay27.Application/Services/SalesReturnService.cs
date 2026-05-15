using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Abstractions;
using Quay27.Application.Common;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Cashbook;
using Quay27.Application.Orders;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class SalesReturnService : ISalesReturnService
{
    private readonly ISalesReturnRepository _returns;
    private readonly IProductRepository _products;
    private readonly ICustomerProfileRepository _customers;
    private readonly IUserRepository _users;
    private readonly ISaleChannelRepository _channels;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSalesReturnRequest> _createValidator;
    private readonly ICashbookSyncService _cashbookSync;

    public SalesReturnService(
        ISalesReturnRepository returns,
        IProductRepository products,
        ICustomerProfileRepository customers,
        IUserRepository users,
        ISaleChannelRepository channels,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreateSalesReturnRequest> createValidator,
        ICashbookSyncService cashbookSync)
    {
        _returns = returns;
        _products = products;
        _customers = customers;
        _users = users;
        _channels = channels;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _cashbookSync = cashbookSync;
    }

    public Task<IReadOnlyList<SalesReturnListItemDto>> ListAsync(SalesReturnListQuery query,
        CancellationToken cancellationToken = default) =>
        _returns.ListAsync(query, cancellationToken);

    public async Task<OrderCreatedDto> CreateAsync(CreateSalesReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var vr = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        if (request.SellerUserId.HasValue &&
            await _users.GetByIdAsync(request.SellerUserId.Value, cancellationToken) is null)
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.SellerUserId), "Người bán không tồn tại.") });
        }

        if (request.SaleChannelId.HasValue &&
            !await _channels.ExistsByIdAsync(request.SaleChannelId.Value, cancellationToken))
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.SaleChannelId), "Kênh bán không tồn tại.") });
        }

        CustomerProfile? customer = null;
        if (request.CustomerProfileId.HasValue)
        {
            customer = await _customers.GetByIdAsync(request.CustomerProfileId.Value, cancellationToken);
            if (customer is null || customer.IsDeleted)
            {
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(request.CustomerProfileId), "Khách hàng không tồn tại.") });
            }
        }

        var (returnSubtotal, returnProducts) =
            await ValidateLinesAndAggregateAsync(request.ReturnItems, cancellationToken);
        if (!MoneyMath.EqualsMoney(returnSubtotal, request.ClientReturnSubtotal))
        {
            throw new ValidationException(new[]
                { new ValidationFailure("clientReturnSubtotal", "Tổng tiền hàng trả không khớp."), });
        }

        var returnDiscount = MoneyMath.Round(request.ClientReturnDiscountAmount);
        if (!MoneyMath.EqualsMoney(returnDiscount, request.ClientReturnDiscountAmount))
        {
            throw new ValidationException(new[]
                { new ValidationFailure("clientReturnDiscountAmount", "Giảm giá trả hàng không hợp lệ."), });
        }

        var returnFee = MoneyMath.Round(request.ClientReturnFeeAmount);
        if (!MoneyMath.EqualsMoney(returnFee, request.ClientReturnFeeAmount))
        {
            throw new ValidationException(new[]
                { new ValidationFailure("clientReturnFeeAmount", "Phí trả hàng không hợp lệ."), });
        }

        var refundDue = MoneyMath.Round(returnSubtotal - returnDiscount - returnFee);
        if (!MoneyMath.EqualsMoney(refundDue, request.ClientRefundDueAmount))
        {
            throw new ValidationException(new[]
                { new ValidationFailure("clientRefundDueAmount", "Cần trả khách (trả hàng) không khớp."), });
        }

        var hasExchange = request.ExchangeItems.Count > 0;
        decimal exchangeSubtotal = 0;
        decimal exchangeDiscount = 0;
        Dictionary<Guid, Product>? exchangeProducts = null;
        decimal purchaseDue = 0;
        decimal netDue = refundDue;

        if (hasExchange)
        {
            (exchangeSubtotal, exchangeProducts) =
                await ValidateLinesAndAggregateAsync(request.ExchangeItems, cancellationToken);
            if (!MoneyMath.EqualsMoney(exchangeSubtotal, request.ClientExchangeSubtotal))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure("clientExchangeSubtotal", "Tổng tiền hàng đổi không khớp."), });
            }

            exchangeDiscount = MoneyMath.Round(request.ClientExchangeDiscountAmount);
            if (!MoneyMath.EqualsMoney(exchangeDiscount, request.ClientExchangeDiscountAmount))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure("clientExchangeDiscountAmount", "Giảm giá mua hàng không hợp lệ."), });
            }

            purchaseDue = MoneyMath.Round(exchangeSubtotal - exchangeDiscount);
            if (!MoneyMath.EqualsMoney(purchaseDue, request.ClientPurchaseDueAmount))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure("clientPurchaseDueAmount", "Tổng tiền mua không khớp."), });
            }

            netDue = MoneyMath.Round(purchaseDue - refundDue);
            if (!MoneyMath.EqualsMoney(netDue, request.ClientNetAmountDueFromCustomer))
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure("clientNetAmountDueFromCustomer", "Khách cần trả không khớp."),
                });
            }
        }
        else
        {
            if (request.ClientExchangeSubtotal != 0 || request.ClientExchangeDiscountAmount != 0 ||
                request.ClientPurchaseDueAmount != 0 || request.ClientNetAmountDueFromCustomer != 0)
            {
                throw new ValidationException(new[]
                    { new ValidationFailure("exchangeItems", "Thông tin mua đổi không hợp lệ."), });
            }
        }

        var id = Guid.NewGuid();
        var code = await _returns.GenerateNextCodeAsync(cancellationToken);

        var amountForList = hasExchange ? MoneyMath.Round(netDue) : MoneyMath.Round(refundDue);

        var entity = new SalesReturn
        {
            Id = id,
            Code = code,
            CreatedAtUtc = DateTime.UtcNow,
            CustomerProfileId = customer?.Id,
            CustomerCode = customer?.CustomerCode,
            CustomerName = customer?.CustomerName,
            ReturnType = "quick_return",
            Status = "returned",
            CreatedByUserId = _currentUser.UserId,
            SellerUserId = request.SellerUserId,
            SaleChannelId = request.SaleChannelId,
            Note = request.Note,
            ReturnSubtotalAmount = returnSubtotal,
            ReturnDiscountAmount = returnDiscount,
            ReturnFeeAmount = returnFee,
            RefundDueAmount = MoneyMath.Round(refundDue),
            HasExchangeItems = hasExchange,
            ExchangeDelivery = request.ExchangeDelivery && hasExchange,
            ExchangeSubtotalAmount = MoneyMath.Round(exchangeSubtotal),
            ExchangeDiscountAmount = hasExchange ? exchangeDiscount : 0,
            PurchaseDueAmount = hasExchange ? MoneyMath.Round(purchaseDue) : 0,
            NetAmountDueFromCustomer = hasExchange ? MoneyMath.Round(netDue) : 0,
            Amount = amountForList,
        };

        foreach (var line in request.ReturnItems)
        {
            var p = returnProducts[line.ProductId];
            var lineTotal = MoneyMath.Round(line.Quantity * line.UnitPrice);
            entity.ReturnItems.Add(new SalesReturnItem
            {
                Id = Guid.NewGuid(),
                SalesReturnId = id,
                ProductId = p.Id,
                ProductCode = line.ProductCode.Trim(),
                ProductName = line.ProductName.Trim(),
                Quantity = line.Quantity,
                UnitPrice = MoneyMath.Round(line.UnitPrice),
                LineTotal = lineTotal,
                Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
            });
        }

        if (hasExchange && exchangeProducts is not null)
        {
            foreach (var line in request.ExchangeItems)
            {
                var p = exchangeProducts[line.ProductId];
                var lineTotal = MoneyMath.Round(line.Quantity * line.UnitPrice);
                entity.ExchangeItems.Add(new SalesReturnExchangeItem
                {
                    Id = Guid.NewGuid(),
                    SalesReturnId = id,
                    ProductId = p.Id,
                    ProductCode = line.ProductCode.Trim(),
                    ProductName = line.ProductName.Trim(),
                    Quantity = line.Quantity,
                    UnitPrice = MoneyMath.Round(line.UnitPrice),
                    LineTotal = lineTotal,
                    Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
                });
            }
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _returns.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cashbookSync.SyncAfterSalesReturnStatusAsync(entity, null, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new OrderCreatedDto { Id = id, Code = code };
        }, cancellationToken);
    }

    public async Task PatchStatusAsync(Guid id, PatchOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.Status), "Trạng thái không được để trống.") });
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "returned", "cancelled",
        };
        var next = request.Status.Trim();
        if (!allowed.Contains(next))
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.Status), "Trạng thái trả hàng không hợp lệ.") });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _returns.GetTrackedByIdAsync(id, cancellationToken)
                         ?? throw new NotFoundException("Không tìm thấy phiếu trả hàng.");
            var prev = entity.Status;
            entity.Status = next;
            await _cashbookSync.SyncAfterSalesReturnStatusAsync(entity, prev, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private async Task<(decimal subtotal, Dictionary<Guid, Product> products)> ValidateLinesAndAggregateAsync(
        IList<CreateOrderItemRequest> lines,
        CancellationToken cancellationToken)
    {
        var productLookup = new Dictionary<Guid, Product>();
        var serverSubtotal = 0m;
        foreach (var line in lines)
        {
            var product =
                productLookup.GetValueOrDefault(line.ProductId)
                ?? await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
            {
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(line.ProductId), "Không tìm thấy hàng hóa.") });
            }

            productLookup[line.ProductId] = product;

            if (!string.Equals(product.RowStatus, "active", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(line.ProductId), "Hàng hóa đã ngừng kinh doanh.") });
            }

            if (!string.Equals(line.ProductCode.Trim(), product.Code.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(line.ProductCode), "Mã hàng không khớp với hệ thống.") });
            }

            var lineSum = MoneyMath.Round(line.Quantity * line.UnitPrice);
            serverSubtotal = MoneyMath.Round(serverSubtotal + lineSum);
        }

        return (serverSubtotal, productLookup);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
