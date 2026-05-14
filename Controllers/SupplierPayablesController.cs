using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Suppliers;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/suppliers/{supplierId:guid}/payable-transactions")]
public sealed class SupplierPayablesController : ControllerBase
{
    private readonly ISupplierPayableService _payables;

    public SupplierPayablesController(ISupplierPayableService payables) => _payables = payables;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierPayableTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierPayableTransactionDto>>> List(
        Guid supplierId,
        [FromQuery] int? transactionType,
        CancellationToken cancellationToken)
    {
        var items = await _payables.ListTransactionsAsync(supplierId, transactionType, cancellationToken);
        return Ok(items);
    }

    [HttpGet("open-goods-receipts")]
    [ProducesResponseType(typeof(IReadOnlyList<SupplierOpenGoodsReceiptRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SupplierOpenGoodsReceiptRowDto>>> OpenGoodsReceipts(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var items = await _payables.ListOpenGoodsReceiptsAsync(supplierId, cancellationToken);
        return Ok(items);
    }

    [HttpPost("adjustment")]
    [ProducesResponseType(typeof(SupplierDebtAdjustmentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDebtAdjustmentDto>> CreateAdjustment(
        Guid supplierId,
        [FromBody] CreateSupplierDebtAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _payables.CreateAdjustmentAsync(supplierId, request, cancellationToken);
        return Ok(created);
    }

    [HttpPost("payment")]
    [ProducesResponseType(typeof(SupplierPayablePaymentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierPayablePaymentDto>> CreatePayment(
        Guid supplierId,
        [FromBody] CreateSupplierPayablePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _payables.CreatePaymentAsync(supplierId, request, cancellationToken);
        return Ok(created);
    }

    [HttpPost("payment-discount")]
    [ProducesResponseType(typeof(SupplierPayableDiscountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierPayableDiscountDto>> CreatePaymentDiscount(
        Guid supplierId,
        [FromBody] CreateSupplierPayableDiscountRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _payables.CreateDiscountAsync(supplierId, request, cancellationToken);
        return Ok(created);
    }

    [HttpGet("export/transactions")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTransactions(
        Guid supplierId,
        [FromQuery] int? transactionType,
        CancellationToken cancellationToken)
    {
        var bytes = await _payables.ExportTransactionsExcelAsync(supplierId, transactionType, cancellationToken);
        var fileName = $"CongNoNCC_{supplierId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("export/debt-summary")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDebtSummary(Guid supplierId, CancellationToken cancellationToken)
    {
        var bytes = await _payables.ExportSupplierDebtSnapshotExcelAsync(supplierId, cancellationToken);
        var fileName = $"TomTatNoNCC_{supplierId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
