namespace WarehousePOS.Domain.Enums;

/// <summary>User roles for role-based authorization.</summary>
public enum UserRole
{
    /// <summary>Full access: all management and configuration screens.</summary>
    Admin = 1,

    /// <summary>POS and inventory operations only.</summary>
    Worker = 2
}

/// <summary>Type of a sale transaction.</summary>
public enum SaleType
{
    Retail = 1,
    Wholesale = 2
}

/// <summary>Payment method for a sale or purchase.</summary>
public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Cheque = 3,
    Other = 4
}

/// <summary>Direction of an inventory movement.</summary>
public enum MovementType
{
    /// <summary>Stock received from a purchase.</summary>
    PurchaseReceive = 1,

    /// <summary>Stock deducted from a sale.</summary>
    StockOut = 2,

    /// <summary>Manual positive adjustment (e.g., correction).</summary>
    StockIn = 3,

    /// <summary>Manual negative adjustment (e.g., damaged goods).</summary>
    Adjustment = 4,

    /// <summary>Stock returned from a customer.</summary>
    ReturnIn = 5,

    /// <summary>Stock returned to a supplier.</summary>
    ReturnOut = 6
}

/// <summary>Status of a purchase order.</summary>
public enum PurchaseStatus
{
    Draft = 1,
    Confirmed = 2,
    PartiallyReceived = 3,
    Received = 4,
    Cancelled = 5
}

/// <summary>Status of a sale.</summary>
public enum SaleStatus
{
    Completed = 1,
    Cancelled = 2,
    Returned = 3
}
