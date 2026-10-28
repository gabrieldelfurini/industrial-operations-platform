using IndustrialOps.Domain.ProductionOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialOps.Infrastructure.Persistence.Configurations;

internal sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();

        // Stored as text ("InProgress") instead of an int: readable in SQL and safe if the enum is reordered.
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.PlannedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ProducedQuantity).HasPrecision(18, 3);
        builder.Property(x => x.ScrapQuantity).HasPrecision(18, 3);

        // Restrict: a product/work center that has orders can never be physically deleted.
        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkCenter)
            .WithMany()
            .HasForeignKey(x => x.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reports belong to the order (part of the aggregate). EF writes to the private
        // _reports field, since the public property is read-only.
        builder.HasMany(x => x.Reports)
            .WithOne()
            .HasForeignKey(r => r.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Reports).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Optimistic concurrency using PostgreSQL's built-in xmin system column: if two requests
        // load the same order and both save, the second one fails instead of silently
        // overwriting the first (e.g. two simultaneous production reports). Shadow property,
        // so the domain does not need to know about it.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
