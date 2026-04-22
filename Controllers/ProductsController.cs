using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Products;

namespace Quay27_Be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    public sealed class ImportPriceListForm
    {
        public IFormFile? File { get; set; }
    }

    private readonly IProductService _service;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(IProductService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductListResponse>> List(
        [FromQuery] string? search,
        [FromQuery] string? groupId,
        [FromQuery] string? stock,
        [FromQuery] string? directSale,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] DateTimeOffset? expectedFrom,
        [FromQuery] DateTimeOffset? expectedTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ListAsync(new ProductQuery
        {
            Search = search,
            GroupId = groupId,
            Stock = stock,
            DirectSale = directSale,
            Status = status,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
            ExpectedFrom = expectedFrom,
            ExpectedTo = expectedTo,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductListItemDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ProductListItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductListItemDto>> Create([FromBody] UpsertProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductListItemDto>> Update(Guid id, [FromBody] UpsertProductRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(ProductListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductListItemDto>> Duplicate(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.DuplicateAsync(id, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ProductListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductListItemDto>> UpdateStatus(Guid id, [FromBody] UpdateProductStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStatusAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/group")]
    [ProducesResponseType(typeof(ProductListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductListItemDto>> UpdateGroup(Guid id, [FromBody] UpdateProductGroupRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateGroupAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("groups")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductGroupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductGroupDto>>> ListGroups(CancellationToken cancellationToken)
        => Ok(await _service.ListGroupsAsync(cancellationToken));

    [HttpGet("groups/tree")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductGroupTreeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductGroupTreeDto>>> ListGroupTree(CancellationToken cancellationToken)
        => Ok(await _service.ListGroupTreeAsync(cancellationToken));

    [HttpPost("groups")]
    [ProducesResponseType(typeof(ProductGroupDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductGroupDto>> CreateGroup([FromBody] CreateProductGroupRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateGroupAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListGroups), new { id = created.Id }, created);
    }

    [HttpGet("price-lists")]
    [ProducesResponseType(typeof(IReadOnlyList<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriceListDto>>> ListPriceLists(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
        => Ok(await _service.ListPriceListsAsync(search, cancellationToken));

    [HttpPost("price-lists")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PriceListDto>> CreatePriceList(
        [FromBody] UpsertPriceListRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreatePriceListAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ListPriceLists), new { id = created.Id }, created);
    }

    [HttpPut("price-lists/{id:guid}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDto>> UpdatePriceList(
        Guid id,
        [FromBody] UpsertPriceListRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.UpdatePriceListAsync(id, request, cancellationToken));

    [HttpGet("price-lists/items")]
    [ProducesResponseType(typeof(IReadOnlyList<PriceListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriceListItemDto>>> ListPriceListItems(
        [FromQuery] List<Guid> priceListIds,
        [FromQuery] string? search,
        [FromQuery] string? groupId,
        [FromQuery] string? stock,
        [FromQuery] string? priceOperator,
        [FromQuery] string? comparePrice,
        [FromQuery] decimal? compareValue,
        CancellationToken cancellationToken)
        => Ok(await _service.ListPriceListItemsAsync(new PriceListItemsQuery
        {
            PriceListIds = priceListIds,
            Search = search,
            GroupId = groupId,
            Stock = stock,
            PriceOperator = priceOperator,
            ComparePrice = comparePrice,
            CompareValue = compareValue
        }, cancellationToken));

    [HttpPost("price-lists/{id:guid}/items/add-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddAllProductsToPriceList(
        Guid id,
        [FromBody] AddAllProductsRequest? request,
        CancellationToken cancellationToken)
    {
        await _service.AddAllProductsToPriceListAsync(id, request?.Confirmed == true, cancellationToken);
        return NoContent();
    }

    [HttpPost("price-lists/{id:guid}/items/add-by-groups")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddProductsByGroups(
        Guid id,
        [FromBody] AddProductsByGroupsRequest request,
        CancellationToken cancellationToken)
    {
        await _service.AddProductsByGroupsToPriceListAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("price-lists/{id:guid}/apply-formula")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApplyPriceFormula(
        Guid id,
        [FromBody] ApplyPriceFormulaRequest request,
        CancellationToken cancellationToken)
    {
        await _service.ApplyPriceFormulaAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("price-lists/import/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPriceListImportTemplate(CancellationToken cancellationToken)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, "Templates", "MauFileBangGia.xlsx");
        if (!System.IO.File.Exists(templatePath))
            return NotFound(new { title = "Template not found", detail = "Không tìm thấy file template bảng giá." });

        var bytes = await System.IO.File.ReadAllBytesAsync(templatePath, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MauFileBangGia.xlsx");
    }

    [HttpPost("price-lists/import")]
    [ProducesResponseType(typeof(PriceListImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<PriceListImportResult>> ImportPriceLists(
        [FromForm] ImportPriceListForm form,
        CancellationToken cancellationToken)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new { title = "Invalid file", detail = "Vui lòng chọn file Excel hợp lệ." });

        await using var ms = new MemoryStream();
        await form.File.CopyToAsync(ms, cancellationToken);
        var result = await _service.ImportPriceListAsync(new PriceListImportRequest
        {
            FileBytes = ms.ToArray(),
            FileName = form.File.FileName
        }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("price-lists/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ExportPriceLists(
        [FromQuery] List<Guid> priceListIds,
        [FromQuery] string? search,
        [FromQuery] string? groupId,
        [FromQuery] string? stock,
        [FromQuery] string? priceOperator,
        [FromQuery] string? comparePrice,
        [FromQuery] decimal? compareValue,
        CancellationToken cancellationToken)
    {
        var bytes = await _service.ExportPriceListAsync(new PriceListItemsQuery
        {
            PriceListIds = priceListIds,
            Search = search,
            GroupId = groupId,
            Stock = stock,
            PriceOperator = priceOperator,
            ComparePrice = comparePrice,
            CompareValue = compareValue
        }, cancellationToken);

        if (bytes is null || bytes.Length == 0)
            return NoContent();

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BangGia_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }
}
