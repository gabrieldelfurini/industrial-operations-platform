namespace IndustrialOps.Domain.ProductionOrders;

/// <summary>
/// Lifecycle of a production order:
/// Planned -> Released -> InProgress -> Completed
/// Planned | Released -> Cancelled
/// </summary>
public enum ProductionOrderStatus
{
    Planned,
    Released,
    InProgress,
    Completed,
    Cancelled
}
