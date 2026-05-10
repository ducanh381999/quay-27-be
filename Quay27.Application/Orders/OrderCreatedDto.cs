namespace Quay27.Application.Orders;

public sealed class OrderCreatedDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
}
