using Quay27.Domain.Entities;

namespace Quay27.Application.Products;

public static class ProductGroupTreeBuilder
{
    public static IReadOnlyList<ProductGroupTreeDto> Build(
        IReadOnlyList<ProductGroup> groups,
        IReadOnlyDictionary<Guid, int>? productCounts = null)
    {
        var byParent = groups.ToLookup(x => x.ParentId);
        return BuildRecursive(null);

        IReadOnlyList<ProductGroupTreeDto> BuildRecursive(Guid? parentId)
        {
            var children = byParent[parentId].OrderBy(g => g.Name).ToList();
            if (children.Count == 0)
                return Array.Empty<ProductGroupTreeDto>();

            return children.Select(x =>
            {
                var nested = BuildRecursive(x.Id);
                var selfCount = productCounts?.GetValueOrDefault(x.Id) ?? 0;
                return new ProductGroupTreeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProductCount = selfCount + nested.Sum(c => c.ProductCount),
                    Children = nested
                };
            }).ToList();
        }
    }
}
