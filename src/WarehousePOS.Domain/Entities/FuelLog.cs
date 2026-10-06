using WarehousePOS.Domain.Common;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Fuel receipt log associated with a driver for variable logistical expense tracking.
/// </summary>
public sealed class FuelLog : AggregateRoot
{
    private FuelLog() { }

    public int DriverUserId { get; private set; }
    public User DriverUser { get; private set; } = null!;

    public DateTime FuelDateUtc { get; private set; }
    public decimal Liters { get; private set; }
    public decimal TotalCost { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public static FuelLog Create(
        int driverUserId,
        decimal liters,
        decimal totalCost,
        string receiptNumber,
        DateTime? fuelDateUtc = null)
    {
        if (driverUserId <= 0) throw new ArgumentOutOfRangeException(nameof(driverUserId));
        if (liters <= 0) throw new ArgumentOutOfRangeException(nameof(liters));
        if (totalCost <= 0) throw new ArgumentOutOfRangeException(nameof(totalCost));
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptNumber);

        return new FuelLog
        {
            DriverUserId = driverUserId,
            Liters = liters,
            TotalCost = totalCost,
            ReceiptNumber = receiptNumber.Trim(),
            FuelDateUtc = fuelDateUtc ?? DateTime.UtcNow
        };
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
}
