using FluentValidation;
using FluentValidation.Results;
using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Application.Common.Exceptions;
using IndustrialOps.Domain.Products;
using IndustrialOps.Domain.ProductionOrders;
using IndustrialOps.Domain.WorkCenters;

namespace IndustrialOps.Application.ProductionOrders;

public interface IProductionOrderService
{
    Task<IReadOnlyList<ProductionOrderResponse>> ListAsync(ProductionOrderStatus? status, CancellationToken ct);
    Task<ProductionOrderResponse> GetAsync(Guid id, CancellationToken ct);
    Task<ProductionOrderResponse> CreateAsync(CreateProductionOrderRequest request, CancellationToken ct);
    Task<ProductionOrderResponse> UpdateAsync(Guid id, UpdateProductionOrderRequest request, CancellationToken ct);
    Task<ProductionOrderResponse> ReleaseAsync(Guid id, CancellationToken ct);
    Task<ProductionOrderResponse> StartAsync(Guid id, CancellationToken ct);
    Task<ProductionOrderResponse> CompleteAsync(Guid id, CancellationToken ct);
    Task<ProductionOrderResponse> CancelAsync(Guid id, CancellationToken ct);
    Task<ProductionReportResponse> ReportProductionAsync(Guid id, ReportProductionRequest request, CancellationToken ct);
    Task<IReadOnlyList<ProductionReportResponse>> ListReportsAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Orchestrates the production order use cases: validates input, loads what is needed,
/// delegates the business rules to the <see cref="ProductionOrder"/> aggregate and commits.
/// </summary>
public sealed class ProductionOrderService(
    IProductionOrderRepository orders,
    IProductRepository products,
    IWorkCenterRepository workCenters,
    IUnitOfWork unitOfWork,
    IValidator<CreateProductionOrderRequest> createValidator,
    IValidator<UpdateProductionOrderRequest> updateValidator,
    IValidator<ReportProductionRequest> reportValidator,
    TimeProvider clock) : IProductionOrderService
{
    private DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<ProductionOrderResponse>> ListAsync(ProductionOrderStatus? status, CancellationToken ct)
    {
        var items = await orders.ListAsync(status, ct);
        return items.Select(ProductionOrderResponse.From).ToList();
    }

    public async Task<ProductionOrderResponse> GetAsync(Guid id, CancellationToken ct) =>
        ProductionOrderResponse.From(await LoadAsync(id, ct));

    public async Task<ProductionOrderResponse> CreateAsync(CreateProductionOrderRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var (product, workCenter) = await LoadReferencesAsync(request.ProductId, request.WorkCenterId, ct);

        var sequence = await orders.GetNextOrderSequenceAsync(ct);
        var order = ProductionOrder.Create(
            orderNumber: $"PO-{sequence:D6}",
            product,
            workCenter,
            request.PlannedQuantity,
            request.PlannedStart.UtcDateTime,
            request.PlannedEnd.UtcDateTime,
            UtcNow);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(ct);
        return ProductionOrderResponse.From(order);
    }

    public async Task<ProductionOrderResponse> UpdateAsync(Guid id, UpdateProductionOrderRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        var order = await LoadAsync(id, ct);
        var (product, workCenter) = await LoadReferencesAsync(request.ProductId, request.WorkCenterId, ct);

        order.UpdatePlanning(
            product,
            workCenter,
            request.PlannedQuantity,
            request.PlannedStart.UtcDateTime,
            request.PlannedEnd.UtcDateTime,
            UtcNow);

        await unitOfWork.SaveChangesAsync(ct);
        return ProductionOrderResponse.From(order);
    }

    public Task<ProductionOrderResponse> ReleaseAsync(Guid id, CancellationToken ct) =>
        ChangeStatusAsync(id, (order, now) => order.Release(now), ct);

    public Task<ProductionOrderResponse> StartAsync(Guid id, CancellationToken ct) =>
        ChangeStatusAsync(id, (order, now) => order.Start(now), ct);

    public Task<ProductionOrderResponse> CompleteAsync(Guid id, CancellationToken ct) =>
        ChangeStatusAsync(id, (order, now) => order.Complete(now), ct);

    public Task<ProductionOrderResponse> CancelAsync(Guid id, CancellationToken ct) =>
        ChangeStatusAsync(id, (order, now) => order.Cancel(now), ct);

    public async Task<ProductionReportResponse> ReportProductionAsync(Guid id, ReportProductionRequest request, CancellationToken ct)
    {
        await reportValidator.ValidateAndThrowAsync(request, ct);
        var order = await LoadAsync(id, ct);

        var report = order.ReportProduction(request.GoodQuantity, request.ScrapQuantity, request.Notes, UtcNow);

        await unitOfWork.SaveChangesAsync(ct);
        return ProductionReportResponse.From(report);
    }

    public async Task<IReadOnlyList<ProductionReportResponse>> ListReportsAsync(Guid id, CancellationToken ct)
    {
        await LoadAsync(id, ct); // 404 if the order does not exist
        var reports = await orders.ListReportsAsync(id, ct);
        return reports.Select(ProductionReportResponse.From).ToList();
    }

    private async Task<ProductionOrderResponse> ChangeStatusAsync(
        Guid id, Action<ProductionOrder, DateTime> transition, CancellationToken ct)
    {
        var order = await LoadAsync(id, ct);
        transition(order, UtcNow);
        await unitOfWork.SaveChangesAsync(ct);
        return ProductionOrderResponse.From(order);
    }

    private async Task<ProductionOrder> LoadAsync(Guid id, CancellationToken ct) =>
        await orders.GetByIdAsync(id, ct) ?? throw new NotFoundException("Production order", id);

    /// <summary>
    /// Ids that come in the request body and point to nothing are an input error (400),
    /// not a missing resource in the URL (404).
    /// </summary>
    private async Task<(Product, WorkCenter)> LoadReferencesAsync(Guid productId, Guid workCenterId, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(productId, ct);
        var workCenter = await workCenters.GetByIdAsync(workCenterId, ct);

        var failures = new List<ValidationFailure>();
        if (product is null)
            failures.Add(new ValidationFailure("ProductId", $"Product '{productId}' does not exist."));
        if (workCenter is null)
            failures.Add(new ValidationFailure("WorkCenterId", $"Work center '{workCenterId}' does not exist."));
        if (failures.Count > 0)
            throw new ValidationException(failures);

        return (product!, workCenter!);
    }
}
