using IndustrialOps.Domain.ProductionOrders;

namespace IndustrialOps.Application.ProductionOrders;

// Dates come in as DateTimeOffset so the client must send an explicit offset/UTC ("Z").
// They are normalized to UTC before reaching the domain.
public sealed record CreateProductionOrderRequest(
    Guid ProductId,
    Guid WorkCenterId,
    decimal PlannedQuantity,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd);

public sealed record UpdateProductionOrderRequest(
    Guid ProductId,
    Guid WorkCenterId,
    decimal PlannedQuantity,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd);

public sealed record ReportProductionRequest(decimal GoodQuantity, decimal ScrapQuantity, string? Notes);

public sealed record ProductionOrderResponse(
    Guid Id,
    string OrderNumber,
    Guid ProductId,
    string? ProductCode,
    Guid WorkCenterId,
    string? WorkCenterCode,
    decimal PlannedQuantity,
    decimal ProducedQuantity,
    decimal ScrapQuantity,
    ProductionOrderStatus Status,
    DateTime PlannedStart,
    DateTime PlannedEnd,
    DateTime? ActualStart,
    DateTime? ActualEnd,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ProductionOrderResponse From(ProductionOrder o) =>
        new(o.Id, o.OrderNumber,
            o.ProductId, o.Product?.Code,
            o.WorkCenterId, o.WorkCenter?.Code,
            o.PlannedQuantity, o.ProducedQuantity, o.ScrapQuantity,
            o.Status,
            o.PlannedStart, o.PlannedEnd, o.ActualStart, o.ActualEnd,
            o.CreatedAt, o.UpdatedAt);
}

public sealed record ProductionReportResponse(
    Guid Id,
    Guid ProductionOrderId,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    string? Notes,
    DateTime ReportedAt)
{
    public static ProductionReportResponse From(ProductionReport r) =>
        new(r.Id, r.ProductionOrderId, r.GoodQuantity, r.ScrapQuantity, r.Notes, r.ReportedAt);
}
