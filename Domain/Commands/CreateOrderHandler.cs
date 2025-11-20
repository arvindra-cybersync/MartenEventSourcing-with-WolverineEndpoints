using Domain.Entities;
using Application.Interfaces;

namespace Application.Commands;

public class CreateOrderHandler
{
    private readonly IOrderRepository _repository;

    public CreateOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(string customerName, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var order = Order.Create(id, customerName);
        await _repository.SaveAsync(order, ct);
        return id;
    }

}
