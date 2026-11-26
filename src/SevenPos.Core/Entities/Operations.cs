namespace SevenPos.Core.Entities;

/// <summary>Doc: Sucursal.</summary>
public class Branch : SyncEntity
{
    public required string Name { get; set; }
    /// <summary>Up to 5 comma-separated emails for shift/alert reports.</summary>
    public string? AlertEmails { get; set; }
}

public enum UserRole { Cashier, Manager, Admin }

public class User : SyncEntity
{
    public required string Name { get; set; }
    public required string PinHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Cashier;
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
}

public enum AttendanceType { ClockIn, LunchStart, LunchEnd, ClockOut }

/// <summary>Doc: Asistencias_RRHH (time clock).</summary>
public class Attendance : SyncEntity
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public AttendanceType Type { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Doc: Cajas_Turnos (Corte X / Corte Z). Totals per payment method are
/// NOT stored here; they are computed from the shift's Payments.
/// </summary>
public class Shift : SyncEntity
{
    public Guid BranchId { get; set; }
    public Guid UserId { get; set; }
    public decimal OpeningCash { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    /// <summary>Cash the cashier counts BEFORE seeing the expected amount.</summary>
    public decimal? BlindCashCount { get; set; }
    /// <summary>Negative = short, positive = over.</summary>
    public decimal? CashDifference { get; set; }

    public List<Payment> Payments { get; set; } = [];
    public List<CashWithdrawal> Withdrawals { get; set; } = [];
}

/// <summary>Doc: Salidas de efectivo (supplier payments, expenses, cash pickups).</summary>
public class CashWithdrawal : SyncEntity
{
    public Guid ShiftId { get; set; }
    public decimal Amount { get; set; }
    public required string Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AuditLog : SyncEntity
{
    public Guid? UserId { get; set; }
    public Guid BranchId { get; set; }
    /// <summary>E.g. TICKET_CANCELLED, PRICE_CHANGED, DRAWER_OPENED.</summary>
    public required string Action { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
