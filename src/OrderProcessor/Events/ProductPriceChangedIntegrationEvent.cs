namespace eShop.OrderProcessor.Events;

using eShop.EventBus.Events;

public record ProductPriceChangedIntegrationEvent(int ProductId, decimal NewPrice, decimal OldPrice, decimal TaxRate = 0) : IntegrationEvent;
