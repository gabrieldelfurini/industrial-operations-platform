using System.Globalization;
using FluentValidation;
using IndustrialOps.Application.Products;
using IndustrialOps.Application.ProductionOrders;
using IndustrialOps.Application.WorkCenters;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWorkCenterService, WorkCenterService>();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();

        // Registers every AbstractValidator<T> in this assembly as IValidator<T>.
        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();

        // Validation messages in English regardless of the server's OS language.
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.InvariantCulture;

        return services;
    }
}
