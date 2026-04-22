using Quay27.Application.Products;

namespace Quay27.Application.Abstractions;

public interface IProductService
{
    Task<ProductListResponse> ListAsync(ProductQuery query, CancellationToken cancellationToken = default);
    Task<ProductListItemDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductListItemDto> CreateAsync(UpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductListItemDto> UpdateAsync(Guid id, UpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductListItemDto> DuplicateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductListItemDto> UpdateStatusAsync(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken = default);
    Task<ProductListItemDto> UpdateGroupAsync(Guid id, UpdateProductGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductGroupDto>> ListGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductGroupTreeDto>> ListGroupTreeAsync(CancellationToken cancellationToken = default);
    Task<ProductGroupDto> CreateGroupAsync(CreateProductGroupRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceListDto>> ListPriceListsAsync(string? search, CancellationToken cancellationToken = default);
    Task<PriceListDto> CreatePriceListAsync(UpsertPriceListRequest request, CancellationToken cancellationToken = default);
    Task<PriceListDto> UpdatePriceListAsync(Guid id, UpsertPriceListRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceListItemDto>> ListPriceListItemsAsync(PriceListItemsQuery query, CancellationToken cancellationToken = default);
    Task AddAllProductsToPriceListAsync(Guid priceListId, bool confirmed, CancellationToken cancellationToken = default);
    Task AddProductsByGroupsToPriceListAsync(Guid priceListId, AddProductsByGroupsRequest request, CancellationToken cancellationToken = default);
    Task ApplyPriceFormulaAsync(Guid priceListId, ApplyPriceFormulaRequest request, CancellationToken cancellationToken = default);
    Task<PriceListImportResult> ImportPriceListAsync(PriceListImportRequest request, CancellationToken cancellationToken = default);
    Task<byte[]?> ExportPriceListAsync(PriceListItemsQuery query, CancellationToken cancellationToken = default);
}
