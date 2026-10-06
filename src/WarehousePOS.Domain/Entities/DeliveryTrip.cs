using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Domain.Entities;

/// <summary>
/// Logistical delivery trip assigned to a driver.
/// </summary>
public sealed class DeliveryTrip : AggregateRoot
{
    private DeliveryTrip() { }

    public string TripCode { get; private set; } = string.Empty;
    public int DriverUserId { get; private set; }
    public User DriverUser { get; private set; } = null!;

    public DateTime DispatchedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public double OdometerStartKm { get; private set; }
    public double OdometerEndKm { get; private set; }
    public decimal MileagePayout { get; private set; }
    public decimal BonusPayout { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static DeliveryTrip Create(
        string tripCode,
        int driverUserId,
        double odometerStartKm,
        decimal mileagePayout = 0m,
        decimal bonusPayout = 0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripCode);
        if (driverUserId <= 0) throw new ArgumentOutOfRangeException(nameof(driverUserId));

        return new DeliveryTrip
        {
            TripCode = tripCode.Trim().ToUpperInvariant(),
            DriverUserId = driverUserId,
            DispatchedAtUtc = DateTime.UtcNow,
            Status = DeliveryStatus.Dispatched,
            OdometerStartKm = odometerStartKm,
            MileagePayout = mileagePayout,
            BonusPayout = bonusPayout
        };
    }

    public void CompleteTrip(double odometerEndKm, decimal bonusPayout)
    {
        OdometerEndKm = odometerEndKm;
        BonusPayout = bonusPayout;
        DeliveredAtUtc = DateTime.UtcNow;
        Status = DeliveryStatus.Delivered;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
}
