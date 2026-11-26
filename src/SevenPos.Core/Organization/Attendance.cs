namespace SevenPos.Core.Organization;

public enum AttendanceType { ClockIn, LunchStart, LunchEnd, ClockOut }

/// <summary>Doc: Asistencias_RRHH (time clock).</summary>
public class Attendance : SyncEntity
{
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public AttendanceType Type { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
