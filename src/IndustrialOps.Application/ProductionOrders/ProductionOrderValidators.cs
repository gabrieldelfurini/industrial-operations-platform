using FluentValidation;

namespace IndustrialOps.Application.ProductionOrders;

public sealed class CreateProductionOrderRequestValidator : AbstractValidator<CreateProductionOrderRequest>
{
    public CreateProductionOrderRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WorkCenterId).NotEmpty();
        RuleFor(x => x.PlannedQuantity).GreaterThan(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
        RuleFor(x => x.PlannedStart).NotEmpty();
        RuleFor(x => x.PlannedEnd)
            .NotEmpty()
            .GreaterThan(x => x.PlannedStart).WithMessage("Planned end must be after planned start.");
    }
}

public sealed class UpdateProductionOrderRequestValidator : AbstractValidator<UpdateProductionOrderRequest>
{
    public UpdateProductionOrderRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WorkCenterId).NotEmpty();
        RuleFor(x => x.PlannedQuantity).GreaterThan(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
        RuleFor(x => x.PlannedStart).NotEmpty();
        RuleFor(x => x.PlannedEnd)
            .NotEmpty()
            .GreaterThan(x => x.PlannedStart).WithMessage("Planned end must be after planned start.");
    }
}

public sealed class ReportProductionRequestValidator : AbstractValidator<ReportProductionRequest>
{
    public ReportProductionRequestValidator()
    {
        RuleFor(x => x.GoodQuantity).GreaterThanOrEqualTo(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
        RuleFor(x => x.ScrapQuantity).GreaterThanOrEqualTo(0).PrecisionScale(18, 3, ignoreTrailingZeros: true);
        RuleFor(x => x)
            .Must(x => x.GoodQuantity + x.ScrapQuantity > 0)
            .WithName("Quantities")
            .WithMessage("Good quantity or scrap quantity must be greater than zero.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
