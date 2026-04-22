using Quay27.Application.Products;
using Quay27.Domain.Entities;

namespace Quay27.Products.Tests;

public class ProductGroupTreeBuilderTests
{
    [Fact]
    public void Build_should_create_nested_tree()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var groups = new List<ProductGroup>
        {
            new()
            {
                Id = rootId,
                Name = "Root",
                ParentId = null
            },
            new()
            {
                Id = childId,
                Name = "Child",
                ParentId = rootId
            }
        };

        var tree = ProductGroupTreeBuilder.Build(groups);

        Assert.Single(tree);
        Assert.Equal("Root", tree[0].Name);
        Assert.Single(tree[0].Children);
        Assert.Equal("Child", tree[0].Children[0].Name);
    }
}
