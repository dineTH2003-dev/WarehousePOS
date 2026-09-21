using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence.Configurations;

public sealed class DeliveryTripConfiguration : IEntityTypeConfiguration<DeliveryTrip>
{
    public void Configure(EntityTypeBuilder<DeliveryTrip> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TripCode)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(d => d.TripCode)
               .IsUnique();

        builder.Property(d => d.MileagePayout)
               .HasPrecision(18, 2);

        builder.Property(d => d.BonusPayout)
               .HasPrecision(18, 2);

        builder.HasOne(d => d.DriverUser)
               .WithMany(u => u.DeliveryTrips)
               .HasForeignKey(d => d.DriverUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(d => d.IsActive);
    }
}
