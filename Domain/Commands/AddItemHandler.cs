using Application.Interfaces;

namespace Application.Commands;

public class AddItemHandler
{
    private readonly IOrderRepository _repository;

    public AddItemHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(Guid orderId, string item, CancellationToken ct)
    {
        var order = await _repository.GetAsync(orderId, ct);
        order.AddItem(item);
        await _repository.SaveAsync(order, ct);
    }
}
