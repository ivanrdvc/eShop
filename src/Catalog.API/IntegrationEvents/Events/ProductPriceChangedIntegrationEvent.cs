namespace eShop.Catalog.API.IntegrationEvents.Events;

// Integration Events notes:
// An Event is “something that has happened in the past”, therefore its name has to be past tense
// An Integration Event is an event that can cause side effects to other microservices, Bounded-Contexts or external systems.
// Note: NewPrice and OldPrice represent the base catalog price (tax-exclusive).
// Consumers should apply the regional tax rate from the TaxRate field to compute the final price.
public record ProductPriceChangedIntegrationEvent(int ProductId, decimal NewPrice, decimal OldPrice, decimal TaxRate = 0) : IntegrationEvent;
