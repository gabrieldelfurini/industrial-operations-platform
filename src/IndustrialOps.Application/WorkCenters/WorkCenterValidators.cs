using FluentValidation;

namespace IndustrialOps.Application.WorkCenters;

public sealed class CreateWorkCenterRequestValidator : AbstractValidator<CreateWorkCenterRequest>
{
    public CreateWorkCenterRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(30)
            .Matches("^[A-Za-z0-9-]+$").WithMessage("Code may contain only letters, digits and '-'.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateWorkCenterRequestValidator : AbstractValidator<UpdateWorkCenterRequest>
{
    public UpdateWorkCenterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
