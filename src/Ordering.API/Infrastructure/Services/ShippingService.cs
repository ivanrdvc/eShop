using System.Text.Json;

namespace eShop.Ordering.API.Infrastructure.Services;

/// <summary>
/// Fetches tracking details from the carrier's API.
/// </summary>
public class ShippingService(ILogger<ShippingService> logger)
{
    public async Task<TrackingDetail> GetTrackingDetailAsync(string carrier, string trackingNumber, CancellationToken cancellationToken)
    {
        var client = new HttpClient();

        try
        {
            var url = $"https://api.shipengine.com/v1/tracking?carrier_code={carrier}&tracking_number={trackingNumber}";
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Carrier API returned {StatusCode} for tracking {TrackingNumber}",
                    response.StatusCode, trackingNumber);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<TrackingDetail>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch tracking detail for {TrackingNumber}", trackingNumber);
            return null;
        }
    }
}

public record TrackingDetail(
    string Status,
    string EstimatedDelivery,
    string CurrentLocation);
