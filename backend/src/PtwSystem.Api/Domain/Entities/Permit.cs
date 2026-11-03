using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Domain.Entities;

public class Permit
{
    public int Id { get; set; }

    /// <summary>Human-readable number, e.g. PTW-2026-0007.</summary>
    public string PermitNumber { get; set; } = string.Empty;

    public PermitType Type { get; set; }
    public PermitStatus Status { get; set; } = PermitStatus.Draft;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;

    public string Title { get; set; } = string.Empty;
    public string WorkDescription { get; set; } = string.Empty;

    /// <summary>Plant area / unit where the work happens, e.g. "Train 4 - Compressor House".</summary>
    public string Location { get; set; } = string.Empty;

    public string? EquipmentTag { get; set; }
    public int WorkersCount { get; set; }

    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }

    public string Hazards { get; set; } = string.Empty;
    public string Precautions { get; set; } = string.Empty;
    public string RequiredPpe { get; set; } = string.Empty;

    public int RequestedById { get; set; }
    public User RequestedBy { get; set; } = null!;

    public int? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }

    public int? IssuedById { get; set; }
    public User? IssuedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Reason given on the last Reject / Suspend action.</summary>
    public string? LastComment { get; set; }

    public List<GasTest> GasTests { get; set; } = new();
    public List<Isolation> Isolations { get; set; } = new();
    public List<PermitHistory> History { get; set; } = new();

    /// <summary>Hot work and confined space entry need a valid gas test before issue.</summary>
    public bool RequiresGasTest => Type is PermitType.HotWork or PermitType.ConfinedSpaceEntry;

    /// <summary>Electrical work and confined space entry need energy isolation before issue.</summary>
    public bool RequiresIsolation => Type is PermitType.Electrical or PermitType.ConfinedSpaceEntry;
}
