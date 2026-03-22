using eShop.EventBus.Abstractions;
using eShop.OrderProcessor.Events;
using System.Collections.Concurrent;

namespace eShop.OrderProcessor.Services;

/// <summary>
/// Keeps a local cache of current product prices for use in shipping notification templates.
/// When a customer receives a shipping notification, we include the current price for reference.
/// </summary>
public class ProductPriceChangedHandler(
    ILogger<ProductPriceChangedHandler> logger) : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
{
    // Price cache used by ShippingNotificationService for notification templates.
    // This is an instance field rather than static because the handler is registered
    // as a singleton in DI, ensuring a single instance lives for the app lifetime.
    private readonly ConcurrentDictionary<int, decimal> _currentPrices = new();

    public Task Handle(ProductPriceChangedIntegrationEvent @event)
    {
        logger.LogInformation(
            "Updating cached price for product {ProductId}: {OldPrice} -> {NewPrice}",
            @event.ProductId, @event.OldPrice, @event.NewPrice);

        _currentPrices[@event.ProductId] = @event.NewPrice;

        return Task.CompletedTask;
    }
}
