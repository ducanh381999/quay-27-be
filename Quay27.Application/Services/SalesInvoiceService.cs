using System.Globalization;
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

public sealed class SalesInvoiceService : ISalesInvoiceService
{
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IProductRepository _products;
    private readonly ICustomerProfileRepository _customers;
    private readonly IUserRepository _users;
    private readonly ISaleChannelRepository _channels;
    private readonly IPriceListRepository _priceLists;
    private readonly IReceivingAccountRepository _receivingAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateSalesInvoiceRequest> _createValidator;
    private readonly ICashbookSyncService _cashbookSync;
    private readonly ICustomerService _customerSheet;

    public SalesInvoiceService(
        ISalesInvoiceRepository invoices,
        IProductRepository products,
        ICustomerProfileRepository customers,
        IUserRepository users,
        ISaleChannelRepository channels,
        IPriceListRepository priceLists,
        IReceivingAccountRepository receivingAccounts,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IValidator<CreateSalesInvoiceRequest> createValidator,
        ICashbookSyncService cashbookSync,
        ICustomerService customerSheet)
    {
        _invoices = invoices;
        _products = products;
        _customers = customers;
        _users = users;
        _channels = channels;
        _priceLists = priceLists;
        _receivingAccounts = receivingAccounts;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _cashbookSync = cashbookSync;
        _customerSheet = customerSheet;
    }

    public Task<IReadOnlyList<SalesInvoiceListItemDto>> ListAsync(SalesInvoiceListQuery query,
        CancellationToken cancellationToken = default) =>
        _invoices.ListAsync(query, cancellationToken);

    public async Task<OrderCreatedDto> CreateAsync(CreateSalesInvoiceRequest request,
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

        if (request.PriceListId.HasValue &&
            await _priceLists.GetByIdAsync(request.PriceListId.Value, cancellationToken) is null)
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.PriceListId), "Bảng giá không tồn tại.") });
        }

        var (paymentMethod, receivingAccountId) = await OrderReceivingAccountResolver.ResolveAsync(
            request.PaymentMethod,
            request.ReceivingAccountId,
            _receivingAccounts,
            cancellationToken);

        var (serverSubtotal, productLookup) = await ValidateLinesAndAggregateAsync(request.Items, cancellationToken);
        OrderTotalsValidation.ValidateSubtotalDiscountTotal(
            serverSubtotal,
            request.ClientSubtotal,
            request.ClientDiscountAmount,
            request.ClientTotal);

        var id = Guid.NewGuid();
        var code = await _invoices.GenerateNextCodeAsync(cancellationToken);
        var discount = MoneyMath.Round(request.ClientDiscountAmount);
        var entity = new SalesInvoice
        {
            Id = id,
            Code = code,
            CreatedAtUtc = DateTime.UtcNow,
            InvoiceDeliveryType = "no_delivery",
            Status = "processing",
            SubtotalAmount = serverSubtotal,
            DiscountAmount = discount,
            PaidAmount = MoneyMath.Round(request.PaidAmount),
            PaymentMethod = paymentMethod,
            PriceListId = request.PriceListId,
            ReceivingAccountId = receivingAccountId,
            SellerUserId = request.SellerUserId,
            CreatedByUserId = _currentUser.UserId,
            SaleChannelId = request.SaleChannelId,
            CustomerProfileId = customer?.Id,
            CustomerCode = customer?.CustomerCode,
            CustomerName = customer?.CustomerName,
            Note = request.Note,
        };

        foreach (var line in request.Items)
        {
            var p = productLookup[line.ProductId];
            var lineTotal = MoneyMath.Round(line.Quantity * line.UnitPrice);
            entity.Items.Add(new SalesInvoiceItem
            {
                Id = Guid.NewGuid(),
                SalesInvoiceId = id,
                ProductId = p.Id,
                ProductCode = line.ProductCode.Trim(),
                ProductName = line.ProductName.Trim(),
                Quantity = line.Quantity,
                UnitPrice = MoneyMath.Round(line.UnitPrice),
                LineTotal = lineTotal,
                Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim(),
            });
        }

        string? sellerStaffLabel = null;
        if (request.SellerUserId is { } sellerId &&
            await _users.GetByIdAsync(sellerId, cancellationToken) is { } sellerUser)
        {
            sellerStaffLabel = string.IsNullOrWhiteSpace(sellerUser.FullName)
                ? sellerUser.Username
                : sellerUser.FullName.Trim();
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _invoices.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _customerSheet.CreateFullSheetRowFromSalesInvoiceAsync(entity, customer, sellerStaffLabel,
                cancellationToken);
            var sheetDateIso = VietnamDate.TodayInVietnam().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new OrderCreatedDto
            {
                Id = id,
                Code = code,
                CustomerSheetDateIso = sheetDateIso,
            };
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
            "processing", "completed", "undeliverable", "cancelled",
        };
        var next = request.Status.Trim();
        if (!allowed.Contains(next))
        {
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(request.Status), "Trạng thái hóa đơn không hợp lệ.") });
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _invoices.GetTrackedByIdAsync(id, cancellationToken)
                         ?? throw new NotFoundException("Không tìm thấy hóa đơn.");
            var prev = entity.Status;
            entity.Status = next;
            await _cashbookSync.SyncAfterSalesInvoiceStatusAsync(entity, prev, cancellationToken);
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
