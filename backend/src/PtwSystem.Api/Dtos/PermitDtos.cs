using System.ComponentModel.DataAnnotations;
using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Dtos;

/// <summary>Used for both create and update (update is only allowed while the permit is a Draft).</summary>
public class PermitRequest
{
    [Required, EnumDataType(typeof(PermitType))]
    public PermitType Type { get; set; }

    [Required, EnumDataType(typeof(RiskLevel))]
    public RiskLevel RiskLevel { get; set; }

    [Required, StringLength(150, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(2000, MinimumLength = 10)]
    public string WorkDescription { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(50)]
    public string? EquipmentTag { get; set; }

    [Range(1, 100)]
    public int WorkersCount { get; set; } = 1;

    [Required]
    public DateTime PlannedStart { get; set; }

    [Required]
    public DateTime PlannedEnd { get; set; }

    [Required, StringLength(1000)]
    public string Hazards { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Precautions { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string RequiredPpe { get; set; } = string.Empty;
}

public class PermitActionRequest
{
    [Required, EnumDataType(typeof(PermitAction))]
    public PermitAction Action { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}

public class PermitQuery
{
    public PermitStatus? Status { get; set; }
    public PermitType? Type { get; set; }
    public string? Search { get; set; }

    /// <summary>Only permits raised by the current user.</summary>
    public bool Mine { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PermitSummaryDto
{
    public int Id { get; set; }
    public string PermitNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PermitType Type { get; set; }
    public PermitStatus Status { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class PermitDetailDto : PermitSummaryDto
{
    public string WorkDescription { get; set; } = string.Empty;
    public string? EquipmentTag { get; set; }
    public int WorkersCount { get; set; }
    public string Hazards { get; set; } = string.Empty;
    public string Precautions { get; set; } = string.Empty;
    public string RequiredPpe { get; set; } = string.Empty;
    public int RequestedById { get; set; }
    public string? ApprovedBy { get; set; }
    public string? IssuedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? LastComment { get; set; }
    public bool RequiresGasTest { get; set; }
    public bool RequiresIsolation { get; set; }

    /// <summary>Workflow actions the current user may perform - the UI uses this to show buttons.</summary>
    public IReadOnlyList<PermitAction> AvailableActions { get; set; } = [];
    public bool CanEdit { get; set; }
    public bool CanRecordSafetyChecks { get; set; }

    public IReadOnlyList<GasTestDto> GasTests { get; set; } = [];
    public IReadOnlyList<IsolationDto> Isolations { get; set; } = [];
    public IReadOnlyList<PermitHistoryDto> History { get; set; } = [];
}

public class PermitHistoryDto
{
    public string Action { get; set; } = string.Empty;
    public PermitStatus? FromStatus { get; set; }
    public PermitStatus ToStatus { get; set; }
    public string? Comment { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>Another live permit at the same location with an overlapping time window (SIMOPS).</summary>
public class PermitConflictDto
{
    public int Id { get; set; }
    public string PermitNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PermitType Type { get; set; }
    public PermitStatus Status { get; set; }
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
}
