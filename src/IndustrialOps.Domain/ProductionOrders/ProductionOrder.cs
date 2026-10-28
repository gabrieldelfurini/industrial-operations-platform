using IndustrialOps.Domain.Common;
using IndustrialOps.Domain.Products;
using IndustrialOps.Domain.WorkCenters;

namespace IndustrialOps.Domain.ProductionOrders;

/// <summary>
/// Aggregate root: an order to manufacture a quantity of a product on a work center.
/// All state changes go through methods that enforce the lifecycle (state machine),
/// so it is impossible to put an order in an invalid state from outside.
/// </summary>
public sealed class ProductionOrder : Entity
{
    private readonly List<ProductionReport> _reports = [];

    private ProductionOrder()
    {
    }

    private ProductionOrder(string orderNumber, DateTime utcNow) : base(utcNow)
    {
        OrderNumber = orderNumber;
        Status = ProductionOrderStatus.Planned;
    }

    public string OrderNumber { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public Guid WorkCenterId { get; private set; }
    public WorkCenter? WorkCenter { get; private set; }
    public decimal PlannedQuantity { get; private set; }
    public decimal ProducedQuantity { get; private set; }
    public decimal ScrapQuantity { get; private set; }
    public ProductionOrderStatus Status { get; private set; }
    public DateTime PlannedStart { get; private set; }
    public DateTime PlannedEnd { get; private set; }
    public DateTime? ActualStart { get; private set; }
    public DateTime? ActualEnd { get; private set; }
    public IReadOnlyCollection<ProductionReport> Reports => _reports.AsReadOnly();

    public static ProductionOrder Create(
        string orderNumber,
        Product product,
        WorkCenter workCenter,
        decimal plannedQuantity,
        DateTime plannedStart,
        DateTime plannedEnd,
        DateTime utcNow)
    {
        var order = new ProductionOrder(Guard.NotEmpty(orderNumber, nameof(OrderNumber)), utcNow);
        order.SetPlanning(product, workCenter, plannedQuantity, plannedStart, plannedEnd);
        return order;
    }

    /// <summary>Planning data can only change while the order has not been released to the shop floor.</summary>
    public void UpdatePlanning(
        Product product,
        WorkCenter workCenter,
        decimal plannedQuantity,
        DateTime plannedStart,
        DateTime plannedEnd,
        DateTime utcNow)
    {
        EnsureStatus(ProductionOrderStatus.Planned, "update planning of");
        SetPlanning(product, workCenter, plannedQuantity, plannedStart, plannedEnd);
        Touch(utcNow);
    }

    public void Release(DateTime utcNow)
    {
        EnsureStatus(ProductionOrderStatus.Planned, "release");
        Status = ProductionOrderStatus.Released;
        Touch(utcNow);
    }

    public void Start(DateTime utcNow)
    {
        EnsureStatus(ProductionOrderStatus.Released, "start");
        Status = ProductionOrderStatus.InProgress;
        ActualStart = utcNow;
        Touch(utcNow);
    }

    public ProductionReport ReportProduction(decimal goodQuantity, decimal scrapQuantity, string? notes, DateTime utcNow)
    {
        EnsureStatus(ProductionOrderStatus.InProgress, "report production for");
        Guard.NotNegative(goodQuantity, "Good quantity");
        Guard.NotNegative(scrapQuantity, "Scrap quantity");
        if (goodQuantity + scrapQuantity == 0)
            throw new DomainException("A production report must have a good or scrap quantity greater than zero.");

        var report = new ProductionReport(Id, goodQuantity, scrapQuantity, notes, utcNow);
        _reports.Add(report);
        ProducedQuantity += goodQuantity;
        ScrapQuantity += scrapQuantity;
        Touch(utcNow);
        return report;
    }

    public void Complete(DateTime utcNow)
    {
        EnsureStatus(ProductionOrderStatus.InProgress, "complete");
        Status = ProductionOrderStatus.Completed;
        ActualEnd = utcNow;
        Touch(utcNow);
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status is not (ProductionOrderStatus.Planned or ProductionOrderStatus.Released))
            throw new DomainException(
                $"Cannot cancel production order '{OrderNumber}' because it is {Status}. Only Planned or Released orders can be cancelled.");

        Status = ProductionOrderStatus.Cancelled;
        Touch(utcNow);
    }

    private void SetPlanning(Product product, WorkCenter workCenter, decimal plannedQuantity, DateTime plannedStart, DateTime plannedEnd)
    {
        if (!product.IsActive)
            throw new DomainException($"Product '{product.Code}' is inactive and cannot be used in new production orders.");
        if (!workCenter.IsActive)
            throw new DomainException($"Work center '{workCenter.Code}' is inactive and cannot be used in new production orders.");
        if (plannedEnd <= plannedStart)
            throw new DomainException("Planned end must be after planned start.");

        ProductId = product.Id;
        Product = product;
        WorkCenterId = workCenter.Id;
        WorkCenter = workCenter;
        PlannedQuantity = Guard.Positive(plannedQuantity, "Planned quantity");
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
    }

    private void EnsureStatus(ProductionOrderStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException(
                $"Cannot {action} production order '{OrderNumber}' because it is {Status}. Expected status: {expected}.");
    }
}
