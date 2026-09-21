using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Technical repair/service ticket assigned to a technician linked to a product.
/// </summary>
public sealed class ServiceTicket : AggregateRoot
{
    private ServiceTicket() { }

    public string TicketCode { get; private set; } = string.Empty;
    public int TechnicianUserId { get; private set; }
    public User TechnicianUser { get; private set; } = null!;

    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;

    public string DefectDescription { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public DateTime CreatedDateUtc { get; private set; }
    public DateTime? ResolvedDateUtc { get; private set; }
    public double TurnaroundHours { get; private set; }

    public decimal RepairLaborFee { get; private set; }
    public decimal SparePartsCost { get; private set; }
    public decimal TechnicianBonus { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static ServiceTicket Create(
        string ticketCode,
        int technicianUserId,
        int productId,
        string defectDescription,
        decimal repairLaborFee = 0m,
        decimal sparePartsCost = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketCode);
        if (technicianUserId <= 0) throw new ArgumentOutOfRangeException(nameof(technicianUserId));
        if (productId <= 0) throw new ArgumentOutOfRangeException(nameof(productId));
        ArgumentException.ThrowIfNullOrWhiteSpace(defectDescription);

        return new ServiceTicket
        {
            TicketCode = ticketCode.Trim().ToUpperInvariant(),
            TechnicianUserId = technicianUserId,
            ProductId = productId,
            DefectDescription = defectDescription.Trim(),
            Status = TicketStatus.Open,
            CreatedDateUtc = DateTime.UtcNow,
            RepairLaborFee = repairLaborFee,
            SparePartsCost = sparePartsCost
        };
    }

    public void ResolveTicket(double turnaroundHours, decimal technicianBonus)
    {
        TurnaroundHours = turnaroundHours;
        TechnicianBonus = technicianBonus;
        ResolvedDateUtc = DateTime.UtcNow;
        Status = TicketStatus.Resolved;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
}
