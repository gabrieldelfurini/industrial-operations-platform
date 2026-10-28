using IndustrialOps.Application.ProductionOrders;
using IndustrialOps.Domain.ProductionOrders;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialOps.Api.Controllers;

[ApiController]
[Route("api/production-orders")]
[Produces("application/json")]
public sealed class ProductionOrdersController(IProductionOrderService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProductionOrderResponse>> List([FromQuery] ProductionOrderStatus? status, CancellationToken ct) =>
        service.ListAsync(status, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProductionOrderResponse> Get(Guid id, CancellationToken ct) =>
        service.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductionOrderResponse>> Create(CreateProductionOrderRequest request, CancellationToken ct)
    {
        var order = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>Changes planning data. Only allowed while the order is Planned.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ProductionOrderResponse> Update(Guid id, UpdateProductionOrderRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    // Lifecycle transitions are explicit actions (POST) instead of a generic "PATCH status":
    // each one has its own rules, and the URL says exactly what happens.

    [HttpPost("{id:guid}/release")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ProductionOrderResponse> Release(Guid id, CancellationToken ct) => service.ReleaseAsync(id, ct);

    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ProductionOrderResponse> Start(Guid id, CancellationToken ct) => service.StartAsync(id, ct);

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ProductionOrderResponse> Complete(Guid id, CancellationToken ct) => service.CompleteAsync(id, ct);

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<ProductionOrderResponse> Cancel(Guid id, CancellationToken ct) => service.CancelAsync(id, ct);

    [HttpGet("{id:guid}/reports")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<ProductionReportResponse>> ListReports(Guid id, CancellationToken ct) =>
        service.ListReportsAsync(id, ct);

    /// <summary>Confirms produced quantities (good and scrap). Only allowed while InProgress.</summary>
    [HttpPost("{id:guid}/reports")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductionReportResponse>> ReportProduction(Guid id, ReportProductionRequest request, CancellationToken ct)
    {
        var report = await service.ReportProductionAsync(id, request, ct);
        return CreatedAtAction(nameof(ListReports), new { id }, report);
    }
}
