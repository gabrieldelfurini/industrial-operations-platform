using FluentValidation;

namespace IndustrialOps.Application.Products;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(30)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Code may contain only letters, digits and '-'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(10);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(10);
    }
}
