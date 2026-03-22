namespace eShop.Ordering.Infrastructure.Repositories;

public class OrderRepository
    : IOrderRepository
{
    private readonly OrderingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public OrderRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Order Add(Order order)
    {
        return _context.Orders.Add(order).Entity;

    }

    public async Task<Order> GetAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order != null)
        {
            await _context.Entry(order)
                .Collection(i => i.OrderItems).LoadAsync();
        }

        return order;
    }

    public void Update(Order order)
    {
        _context.Entry(order).State = EntityState.Modified;
    }

    /// <summary>
    /// Returns orders that are eligible for shipping based on fulfillment criteria.
    /// </summary>
    public async Task<IReadOnlyList<Order>> GetShippableOrdersAsync()
    {
        var minimumOrderValue = 10.00m;
        var maxOrderAgeDays = 30;
        var cutoffDate = DateTime.UtcNow.AddDays(-maxOrderAgeDays);

        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderStatus == OrderStatus.Paid)
            .Where(o => o.OrderDate >= cutoffDate)
            .Where(o => o.OrderItems.Count > 0)
            .Where(o => o.OrderItems.Sum(i => i.Units * i.UnitPrice) >= minimumOrderValue)
            .ToListAsync();
    }
}
