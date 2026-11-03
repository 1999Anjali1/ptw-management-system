using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Domain.Entities;

/// <summary>Energy isolation (LOTO) applied for a permit.</summary>
public class Isolation
{
    public int Id { get; set; }

    public int PermitId { get; set; }
    public Permit Permit { get; set; } = null!;

    public IsolationType Type { get; set; }

    /// <summary>Isolation point, e.g. breaker / valve tag.</summary>
    public string IsolationPoint { get; set; } = string.Empty;

    public string? LockNumber { get; set; }
    public string? Description { get; set; }

    public bool IsRemoved { get; set; }

    public int AppliedById { get; set; }
    public User AppliedBy { get; set; } = null!;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public int? RemovedById { get; set; }
    public User? RemovedBy { get; set; }
    public DateTime? RemovedAt { get; set; }
}
