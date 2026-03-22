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
    // Shared price cache used by ShippingNotificationService for notification templates
    public static readonly ConcurrentDictionary<int, decimal> CurrentPrices = new();

    public Task Handle(ProductPriceChangedIntegrationEvent @event)
    {
        logger.LogInformation(
            "Updating cached price for product {ProductId}: {OldPrice} -> {NewPrice}",
            @event.ProductId, @event.OldPrice, @event.NewPrice);

        CurrentPrices[@event.ProductId] = @event.NewPrice;

        return Task.CompletedTask;
    }
}
