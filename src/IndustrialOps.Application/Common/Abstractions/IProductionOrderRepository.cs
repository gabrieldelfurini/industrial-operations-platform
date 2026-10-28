using IndustrialOps.Domain.ProductionOrders;

namespace IndustrialOps.Application.Common.Abstractions;

public interface IProductionOrderRepository
{
    Task<IReadOnlyList<ProductionOrder>> ListAsync(ProductionOrderStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Loads the order with its product and work center (no reports).</summary>
    Task<ProductionOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionReport>> ListReportsAsync(Guid productionOrderId, CancellationToken cancellationToken = default);

    /// <summary>Next value of a database sequence, used to build human-friendly order numbers.</summary>
    Task<long> GetNextOrderSequenceAsync(CancellationToken cancellationToken = default);

    void Add(ProductionOrder order);
}
