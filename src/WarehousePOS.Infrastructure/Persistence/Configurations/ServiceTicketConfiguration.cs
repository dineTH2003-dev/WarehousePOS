using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehousePOS.Domain.Entities;

namespace WarehousePOS.Infrastructure.Persistence.Configurations;

public sealed class ServiceTicketConfiguration : IEntityTypeConfiguration<ServiceTicket>
{
    public void Configure(EntityTypeBuilder<ServiceTicket> builder)
    {
        builder.HasKey(st => st.Id);

        builder.Property(st => st.TicketCode)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasIndex(st => st.TicketCode)
               .IsUnique();

        builder.Property(st => st.DefectDescription)
               .IsRequired()
               .HasMaxLength(1000);

        builder.Property(st => st.RepairLaborFee)
               .HasPrecision(18, 2);

        builder.Property(st => st.SparePartsCost)
               .HasPrecision(18, 2);

        builder.Property(st => st.TechnicianBonus)
               .HasPrecision(18, 2);

        builder.HasOne(st => st.TechnicianUser)
               .WithMany(u => u.ServiceTickets)
               .HasForeignKey(st => st.TechnicianUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(st => st.Product)
               .WithMany()
               .HasForeignKey(st => st.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(st => st.IsActive);
    }
}
