using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Domain.Entities;

/// <summary>Audit trail entry - every status change and safety record is logged.</summary>
public class PermitHistory
{
    public int Id { get; set; }

    public int PermitId { get; set; }
    public Permit Permit { get; set; } = null!;

    public string Action { get; set; } = string.Empty;
    public PermitStatus? FromStatus { get; set; }
    public PermitStatus ToStatus { get; set; }
    public string? Comment { get; set; }

    public int PerformedById { get; set; }
    public User PerformedBy { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
