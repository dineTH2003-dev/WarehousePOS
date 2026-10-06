using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence.Configurations;

public sealed class SupplierProductEntitlementConfiguration : IEntityTypeConfiguration<SupplierProductEntitlement>
{
    public void Configure(EntityTypeBuilder<SupplierProductEntitlement> builder)
    {
        builder.ToTable("SupplierProductEntitlements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nature)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Value)
            .HasPrecision(18, 2);

        builder.Property(e => e.SpecialNotes)
            .HasMaxLength(1000);

        builder.HasOne(e => e.Supplier)
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
