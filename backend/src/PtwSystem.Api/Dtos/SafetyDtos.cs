using System.ComponentModel.DataAnnotations;
using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Dtos;

public class GasTestRequest
{
    [Range(0, 100)]
    public double OxygenPercent { get; set; }

    [Range(0, 100)]
    public double LelPercent { get; set; }

    [Range(0, 1000)]
    public double H2sPpm { get; set; }

    [Range(0, 1000)]
    public double CoPpm { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}

public class GasTestDto
{
    public int Id { get; set; }
    public double OxygenPercent { get; set; }
    public double LelPercent { get; set; }
    public double H2sPpm { get; set; }
    public double CoPpm { get; set; }
    public bool IsPassed { get; set; }
    public string? Remarks { get; set; }
    public string TestedBy { get; set; } = string.Empty;
    public DateTime TestedAt { get; set; }
    public IReadOnlyList<string> Failures { get; set; } = [];
}

public class IsolationRequest
{
    [Required, EnumDataType(typeof(IsolationType))]
    public IsolationType Type { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string IsolationPoint { get; set; } = string.Empty;

    [StringLength(30)]
    public string? LockNumber { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

public class IsolationDto
{
    public int Id { get; set; }
    public IsolationType Type { get; set; }
    public string IsolationPoint { get; set; } = string.Empty;
    public string? LockNumber { get; set; }
    public string? Description { get; set; }
    public bool IsRemoved { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public string? RemovedBy { get; set; }
    public DateTime? RemovedAt { get; set; }
}

public class DashboardSummaryDto
{
    public int TotalPermits { get; set; }
    public int ActivePermits { get; set; }
    public int PendingApproval { get; set; }
    public int ReadyToIssue { get; set; }
    public int Suspended { get; set; }
    public int Overdue { get; set; }
    public int OpenIsolations { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
    public IReadOnlyList<PermitSummaryDto> ActiveNow { get; set; } = [];
}
