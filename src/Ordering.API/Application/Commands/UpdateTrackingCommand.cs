namespace eShop.Ordering.API.Application.Commands;

public record UpdateTrackingCommand(int OrderNumber, string TrackingNumber, string Carrier) : IRequest<bool>;

public class UpdateTrackingCommandHandler(
    IOrderRepository orderRepository) : IRequestHandler<UpdateTrackingCommand, bool>
{
    public async Task<bool> Handle(UpdateTrackingCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetAsync(request.OrderNumber);

        if (order is null)
        {
            return false;
        }

        order.SetTrackingInfo(request.TrackingNumber, request.Carrier);

        return await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}
