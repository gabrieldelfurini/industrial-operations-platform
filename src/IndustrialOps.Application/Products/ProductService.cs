using FluentValidation;
using IndustrialOps.Application.Common.Abstractions;
using IndustrialOps.Application.Common.Exceptions;
using IndustrialOps.Domain.Products;

namespace IndustrialOps.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<ProductResponse> GetAsync(Guid id, CancellationToken ct);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct);
    Task DeactivateAsync(Guid id, CancellationToken ct);
}

public sealed class ProductService(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    TimeProvider clock) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var items = await products.ListAsync(includeInactive, ct);
        return items.Select(ProductResponse.From).ToList();
    }

    public async Task<ProductResponse> GetAsync(Guid id, CancellationToken ct) =>
        ProductResponse.From(await LoadAsync(id, ct));

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await products.CodeExistsAsync(code, ct))
            throw new ConflictException($"A product with code '{code}' already exists.");

        var product = new Product(code, request.Name, request.UnitOfMeasure, clock.GetUtcNow().UtcDateTime);
        products.Add(product);
        await unitOfWork.SaveChangesAsync(ct);
        return ProductResponse.From(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var product = await LoadAsync(id, ct);
        product.Update(request.Name, request.UnitOfMeasure, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return ProductResponse.From(product);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct)
    {
        var product = await LoadAsync(id, ct);
        product.Deactivate(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Product> LoadAsync(Guid id, CancellationToken ct) =>
        await products.GetByIdAsync(id, ct) ?? throw new NotFoundException("Product", id);
}
