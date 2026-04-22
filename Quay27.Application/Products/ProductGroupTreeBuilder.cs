using Quay27.Domain.Entities;

namespace Quay27.Application.Products;

public static class ProductGroupTreeBuilder
{
    public static IReadOnlyList<ProductGroupTreeDto> Build(IReadOnlyList<ProductGroup> groups)
    {
        var byParent = groups.ToLookup(x => x.ParentId);
        return BuildRecursive(null);

        IReadOnlyList<ProductGroupTreeDto> BuildRecursive(Guid? parentId)
        {
            var children = byParent[parentId].OrderBy(g => g.Name).ToList();
            if (children.Count == 0)
                return Array.Empty<ProductGroupTreeDto>();

            return children.Select(x => new ProductGroupTreeDto
            {
                Id = x.Id,
                Name = x.Name,
                Children = BuildRecursive(x.Id)
            }).ToList();
        }
    }
}
