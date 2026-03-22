using System.Collections.Concurrent;

namespace eShop.Ordering.API.Infrastructure.Services;

/// <summary>
/// Caches recent tracking info to reduce database lookups on the order detail page.
/// </summary>
public class OrderTrackingCache(
    IOrderRepository orderRepository,
    ILogger<OrderTrackingCache> logger)
{
    private readonly ConcurrentDictionary<int, TrackingSnapshot> _cache = new();

    public async Task<TrackingSnapshot> GetTrackingAsync(int orderId)
    {
        if (_cache.TryGetValue(orderId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            return cached;
        }

        var order = await orderRepository.GetAsync(orderId);
        if (order?.TrackingNumber is null)
        {
            return null;
        }

        var snapshot = new TrackingSnapshot(order.TrackingNumber, order.Carrier, DateTime.UtcNow.AddMinutes(5));
        _cache[orderId] = snapshot;

        logger.LogDebug("Cached tracking for order {OrderId}: {TrackingNumber}", orderId, order.TrackingNumber);
        return snapshot;
    }

    public void Invalidate(int orderId) => _cache.TryRemove(orderId, out _);
}

public record TrackingSnapshot(string TrackingNumber, string Carrier, DateTime ExpiresAt);
