using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence.Configurations;

public sealed class FuelLogConfiguration : IEntityTypeConfiguration<FuelLog>
{
    public void Configure(EntityTypeBuilder<FuelLog> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.ReceiptNumber)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(f => f.Liters)
               .HasPrecision(18, 2);

        builder.Property(f => f.TotalCost)
               .HasPrecision(18, 2);

        builder.HasOne(f => f.DriverUser)
               .WithMany(u => u.FuelLogs)
               .HasForeignKey(f => f.DriverUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(f => f.IsActive);
    }
}
