using System.Text.Json.Serialization;
using eShop.OrderProcessor.Events;
using eShop.OrderProcessor.Services;

namespace eShop.OrderProcessor.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddRabbitMqEventBus("eventbus")
               .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventContext.Default))
               .AddSubscription<ProductPriceChangedIntegrationEvent, ProductPriceChangedHandler>();

        builder.AddNpgsqlDataSource("orderingdb");

        builder.Services.AddOptions<BackgroundTaskOptions>()
            .BindConfiguration(nameof(BackgroundTaskOptions));

        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<NotificationDispatcher>();
        builder.Services.AddHostedService<GracePeriodManagerService>();
        builder.Services.AddHostedService<ShippingNotificationService>();
    }
}

[JsonSerializable(typeof(GracePeriodConfirmedIntegrationEvent))]
[JsonSerializable(typeof(ProductPriceChangedIntegrationEvent))]
partial class IntegrationEventContext : JsonSerializerContext
{

}
