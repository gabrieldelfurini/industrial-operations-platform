using IndustrialOps.Domain.ProductionOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialOps.Infrastructure.Persistence.Configurations;

internal sealed class ProductionReportConfiguration : IEntityTypeConfiguration<ProductionReport>
{
    public void Configure(EntityTypeBuilder<ProductionReport> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GoodQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ScrapQuantity).HasPrecision(18, 3);
        builder.Property(x => x.Notes).HasMaxLength(500);
    }
}
