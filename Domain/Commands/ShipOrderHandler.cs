using Application.Interfaces;

namespace Application.Commands;

public class ShipOrderHandler
{
    private readonly IOrderRepository _repository;

    public ShipOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(Guid orderId, CancellationToken ct)
    {
        var order = await _repository.GetAsync(orderId, ct);
        order.Ship();
        await _repository.SaveAsync(order, ct);
    }
}
