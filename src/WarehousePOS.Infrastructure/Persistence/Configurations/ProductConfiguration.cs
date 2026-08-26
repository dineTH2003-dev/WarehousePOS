using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(p => p.SKU)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(p => p.SKU)
               .IsUnique();

        builder.Property(p => p.Barcode)
               .HasMaxLength(100);

        builder.HasIndex(p => p.Barcode)
               .IsUnique()
               .HasFilter("[Barcode] IS NOT NULL");

        builder.Property(p => p.RetailPrice)
               .HasColumnType("decimal(18,2)");

        builder.Property(p => p.WholesalePrice)
               .HasColumnType("decimal(18,2)");

        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
