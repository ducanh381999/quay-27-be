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

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICustomerProfileRepository _customers;
    private readonly IUserRepository _users;
    private readonly ISaleChannelRepository _channels;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreatePurchaseOrderRequest> _createValidator;
    private readonly ICashbookSyncService _cashbookSync;

    public PurchaseOrderService(
        IPurchaseOrderRepository orders,
        IProductRepository products,
        ICustomerProfileRepository customers,
        IUserRepository users,
        ISaleChannelRepository channels,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreatePurchaseOrderRequest> createValidator,
        ICashbookSyncService cashbookSync)
    {
        _orders = orders;
        _products = products;
        _customers = customers;
        _users = users;
        _channels = channels;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _cashbookSync = cashbookSync;
    }

    public Task<IReadOnlyList<PurchaseOrderListItemDto>> ListAsync(PurchaseOrderListQuery query,
        CancellationToken cancellationToken = default) =>
        _orders.ListAsync(query, cancellationToken);

    public async Task<OrderCreatedDto> CreateAsync(CreatePurchaseOrderRequest request,
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

        var productLookup = new Dictionary<Guid, Product>();
        var serverSubtotal = 0m;
        foreach (var line in request.Items)
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

        OrderTotalsValidation.ValidateSubtotalDiscountTotal(
            serverSubtotal, request.ClientSubtotal, request.ClientDiscountAmount, request.ClientTotal);

        var id = Guid.NewGuid();
        var code = await _orders.GenerateNextCodeAsync(cancellationToken);
        var discount = MoneyMath.Round(request.ClientDiscountAmount);
        var amountDue =
            MoneyMath.Round(serverSubtotal - discount); // validated == ClientTotal
        var amountPaid = MoneyMath.Round(request.AmountPaid);

        var entity = new PurchaseOrder
        {
            Id = id,
            Code = code,
            CreatedAtUtc = DateTime.UtcNow,
            CustomerProfileId = customer?.Id,
            CustomerCode = customer?.CustomerCode,
            CustomerName = customer?.CustomerName,
            Status = "draft",
            SubtotalAmount = serverSubtotal,
            DiscountAmount = discount,
            AmountDue = amountDue,
            AmountPaid = amountPaid,
            PaymentMethod = "cash",
            DeliveryFromUtc = request.ScheduledDeliveryUtc,
            SellerUserId = request.SellerUserId,
            CreatedByUserId = _currentUser.UserId,
            SaleChannelId = request.SaleChannelId,
            Note = request.Note,
        };

        foreach (var line in request.Items)
        {
            var p = productLookup[line.ProductId];
            var lineTotal = MoneyMath.Round(line.Quantity * line.UnitPrice);
            entity.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = id,
                ProductId = p.Id,
                ProductCode = line.ProductCode.Trim(),
                ProductName = line.ProductName.Trim(),
                Quantity = line.Quantity,
                UnitPrice = MoneyMath.Round(line.UnitPrice),
                LineTotal = lineTotal,
            });
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _orders.AddAsync(entity, cancellationToken);
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
            "draft", "confirmed", "shipping", "completed", "cancelled",
        };
        var next = request.Status.Trim();
        if (!allowed.Contains(next))
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.Status), "Trạng thái đặt hàng không hợp lệ.") });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _orders.GetTrackedByIdAsync(id, cancellationToken)
                         ?? throw new NotFoundException("Không tìm thấy đặt hàng.");
            var prev = entity.Status;
            entity.Status = next;
            await _cashbookSync.SyncAfterPurchaseOrderStatusAsync(entity, prev, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
