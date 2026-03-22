using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace eShop.OrderProcessor.Services
{
    public class ShippingNotificationService(
        IOptions<BackgroundTaskOptions> options,
        ILogger<ShippingNotificationService> logger,
        NpgsqlDataSource dataSource,
        [FromKeyedServices("catalog")] NpgsqlDataSource catalogDataSource) : BackgroundService
    {
        private readonly BackgroundTaskOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayTime = TimeSpan.FromSeconds(30);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("ShippingNotificationService is starting.");
                stoppingToken.Register(() => logger.LogDebug("ShippingNotificationService background task is stopping."));
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("ShippingNotificationService background task is doing background work.");
                }

                await SendShippingNotifications();

                await Task.Delay(delayTime, stoppingToken);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("ShippingNotificationService background task is stopping.");
            }
        }

        private async Task SendShippingNotifications()
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Checking for shipped orders pending notification");
            }

            var shippedOrders = await GetShippedOrdersPendingNotification();

            foreach (var order in shippedOrders)
            {
                var orderItems = await GetOrderItemProductDetails(order.OrderId);

                logger.LogInformation(
                    "Sending shipping notification for Order {OrderId} to {BuyerEmail} with {ItemCount} items: {Items}",
                    order.OrderId,
                    order.BuyerEmail,
                    orderItems.Count,
                    string.Join(", ", orderItems.Select(i => i.ProductName)));

                await MarkOrderNotificationSent(order.OrderId);
            }
        }

        private async ValueTask<List<ShippedOrderInfo>> GetShippedOrdersPendingNotification()
        {
            try
            {
                using var conn = dataSource.CreateConnection();
                using var command = conn.CreateCommand();
                command.CommandText = """
                    SELECT o."Id", o."Description", b."Name" AS "BuyerName", b."IdentityGuid" AS "BuyerEmail"
                    FROM ordering.orders o
                    INNER JOIN ordering.buyers b ON o."BuyerId" = b."Id"
                    WHERE o."OrderStatus" = 'Shipped' AND o."ShippingNotificationSent" = false
                    """;

                List<ShippedOrderInfo> orders = [];

                await conn.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    orders.Add(new ShippedOrderInfo(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetString(3)));
                }

                return orders;
            }
            catch (NpgsqlException exception)
            {
                logger.LogError(exception, "Fatal error querying ordering database for shipped orders");
            }

            return [];
        }

        /// <summary>
        /// Get product details (name, image) for order items directly from the catalog database.
        /// This avoids the overhead of HTTP calls to the Catalog API for better performance
        /// when we just need product display info for the notification email.
        /// </summary>
        private async ValueTask<List<OrderItemProductInfo>> GetOrderItemProductDetails(int orderId)
        {
            try
            {
                // Get the product IDs from the order items in the ordering DB
                var productIds = new List<int>();
                using (var conn = dataSource.CreateConnection())
                {
                    using var command = conn.CreateCommand();
                    command.CommandText = """
                        SELECT "ProductId", "Units"
                        FROM ordering.orderitems
                        WHERE "OrderId" = @OrderId
                        """;
                    command.Parameters.AddWithValue("OrderId", orderId);

                    await conn.OpenAsync();
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        productIds.Add(reader.GetInt32(0));
                    }
                }

                if (productIds.Count == 0)
                    return [];

                // Query catalog DB directly for product names and images — faster than
                // calling the Catalog API and avoids adding an HTTP dependency here
                using var catalogConn = catalogDataSource.CreateConnection();
                using var catalogCommand = catalogConn.CreateCommand();

                catalogCommand.CommandText = """
                    SELECT "Id", "Name", "PictureFileName"
                    FROM catalog."Catalog"
                    WHERE "Id" = ANY(@ProductIds)
                    """;
                catalogCommand.Parameters.AddWithValue("ProductIds", productIds.ToArray());

                List<OrderItemProductInfo> items = [];

                await catalogConn.OpenAsync();
                using var catalogReader = await catalogCommand.ExecuteReaderAsync();
                while (await catalogReader.ReadAsync())
                {
                    items.Add(new OrderItemProductInfo(
                        catalogReader.GetInt32(0),
                        catalogReader.GetString(1),
                        catalogReader.IsDBNull(2) ? null : catalogReader.GetString(2)));
                }

                return items;
            }
            catch (NpgsqlException exception)
            {
                logger.LogError(exception, "Error fetching product details for order {OrderId}", orderId);
            }

            return [];
        }

        private async Task MarkOrderNotificationSent(int orderId)
        {
            try
            {
                using var conn = dataSource.CreateConnection();
                using var command = conn.CreateCommand();
                command.CommandText = """
                    UPDATE ordering.orders
                    SET "ShippingNotificationSent" = true
                    WHERE "Id" = @OrderId
                    """;
                command.Parameters.AddWithValue("OrderId", orderId);

                await conn.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (NpgsqlException exception)
            {
                logger.LogError(exception, "Error marking notification sent for order {OrderId}", orderId);
            }
        }

        private record ShippedOrderInfo(int OrderId, string Description, string BuyerName, string BuyerEmail);
        private record OrderItemProductInfo(int ProductId, string ProductName, string PictureFileName);
    }
}
