using IndustrialOps.Domain.Common;

namespace IndustrialOps.Domain.ProductionOrders;

/// <summary>
/// A production confirmation ("apontamento"): how many good and scrapped units
/// were produced for an order at a given moment. Created only through
/// <see cref="ProductionOrder.ReportProduction"/>.
/// </summary>
public sealed class ProductionReport : Entity
{
    private ProductionReport()
    {
    }

    internal ProductionReport(Guid productionOrderId, decimal goodQuantity, decimal scrapQuantity, string? notes, DateTime utcNow)
        : base(utcNow)
    {
        ProductionOrderId = productionOrderId;
        GoodQuantity = goodQuantity;
        ScrapQuantity = scrapQuantity;
        Notes = notes?.Trim();
        ReportedAt = utcNow;
    }

    public Guid ProductionOrderId { get; private set; }
    public decimal GoodQuantity { get; private set; }
    public decimal ScrapQuantity { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ReportedAt { get; private set; }
}
