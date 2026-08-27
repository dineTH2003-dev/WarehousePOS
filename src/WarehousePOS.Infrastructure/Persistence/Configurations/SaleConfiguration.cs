using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(c => c.Phone)
               .HasMaxLength(30);

        builder.Property(c => c.Email)
               .HasMaxLength(150);

        builder.Property(c => c.Address)
               .HasMaxLength(500);

        builder.Property(c => c.Type)
               .HasConversion<int>();
    }
}

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SaleType).HasConversion<int>();
        builder.Property(s => s.Status).HasConversion<int>();

        builder.Property(s => s.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(s => s.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(s => s.AmountPaid).HasColumnType("decimal(18,2)");

        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.HasOne(s => s.Customer)
               .WithMany()
               .HasForeignKey(s => s.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Items)
               .WithOne()
               .HasForeignKey(i => i.SaleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(i => i.Discount).HasColumnType("decimal(18,2)");

        builder.HasOne(i => i.Product)
               .WithMany()
               .HasForeignKey(i => i.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
