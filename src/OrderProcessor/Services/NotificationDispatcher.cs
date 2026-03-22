using System.Net.Http.Json;

namespace eShop.OrderProcessor.Services
{
    public class NotificationDispatcher(
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationDispatcher> logger)
    {
        private const string NotificationEndpoint = "https://api.notifications.eshop.internal/v1/notifications";
        private const int MaxRetryAttempts = 3;

        public async Task SendNotificationAsync(int orderId, string buyerEmail, string subject, string body)
        {
            var payload = new
            {
                OrderId = orderId,
                Email = buyerEmail,
                Subject = subject,
                Body = body,
                Timestamp = DateTime.UtcNow
            };

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    logger.LogInformation("Sending notification for order {OrderId}, attempt {Attempt}/{MaxAttempts}",
                        orderId, attempt, MaxRetryAttempts);

                    using var client = httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var response = await client.PostAsJsonAsync(NotificationEndpoint, payload);
                    response.EnsureSuccessStatusCode();

                    logger.LogInformation("Notification sent successfully for order {OrderId} on attempt {Attempt}",
                        orderId, attempt);
                    return;
                }
                catch (HttpRequestException ex) when (attempt < MaxRetryAttempts)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    logger.LogWarning(ex, "Transient error sending notification for order {OrderId} on attempt {Attempt}. Retrying in {Delay}s",
                        orderId, attempt, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }

            logger.LogError("Failed to send notification for order {OrderId} after {MaxAttempts} attempts",
                orderId, MaxRetryAttempts);
            throw new InvalidOperationException($"Failed to send notification for order {orderId} after {MaxRetryAttempts} attempts");
        }
    }
}
