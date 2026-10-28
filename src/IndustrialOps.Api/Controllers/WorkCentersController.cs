using IndustrialOps.Application.WorkCenters;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialOps.Api.Controllers;

[ApiController]
[Route("api/work-centers")]
[Produces("application/json")]
public sealed class WorkCentersController(IWorkCenterService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<WorkCenterResponse>> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        service.ListAsync(includeInactive, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkCenterResponse> Get(Guid id, CancellationToken ct) =>
        service.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkCenterResponse>> Create(CreateWorkCenterRequest request, CancellationToken ct)
    {
        var workCenter = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = workCenter.Id }, workCenter);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<WorkCenterResponse> Update(Guid id, UpdateWorkCenterRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    /// <summary>Soft delete: the work center is deactivated, never physically removed.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }
}
