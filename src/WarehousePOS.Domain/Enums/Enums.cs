namespace WarehousePOS.Domain.Enums;

/// <summary>User roles for role-based authorization.</summary>
public enum UserRole
{
    /// <summary>Full access: all management and configuration screens.</summary>
    Admin = 1,

    /// <summary>POS and inventory operations only.</summary>
    Worker = 2,

    /// <summary>Cashier role for register and checkout access.</summary>
    Cashier = 3
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
    Card = 2,
    Cheque = 3,
    BankTransfer = 4,
    Other = 5
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
    Returned = 3,
    AdvancePaid = 4,
    PartiallyReturned = 5
}

/// <summary>Status of a delivery trip.</summary>
public enum DeliveryStatus
{
    Pending = 1,
    Dispatched = 2,
    Delivered = 3,
    Failed = 4
}

/// <summary>Status of a technical service ticket.</summary>
public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Rejected = 4
}

/// <summary>Classification of expense fixed vs variable.</summary>
public enum ExpenseType
{
    Fixed = 1,
    Variable = 2
}

/// <summary>Operational role of employee/user for payroll consolidation.</summary>
public enum EmployeeRole
{
    Admin = 1,
    Salesman = 2,
    Driver = 3,
    Technician = 4
}

