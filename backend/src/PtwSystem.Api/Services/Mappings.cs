using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Domain.Enums;
using PtwSystem.Api.Domain.Rules;
using PtwSystem.Api.Dtos;

namespace PtwSystem.Api.Services;

/// <summary>Manual entity → DTO mapping. Simple and explicit (no AutoMapper needed).</summary>
public static class Mappings
{
    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, Company = u.Company, IsActive = u.IsActive
    };

    public static bool IsOverdue(this Permit p, DateTime utcNow) =>
        (p.Status is PermitStatus.Active or PermitStatus.Suspended) && p.PlannedEnd < utcNow;

    public static PermitSummaryDto ToSummaryDto(this Permit p, DateTime utcNow) => new()
    {
        Id = p.Id,
        PermitNumber = p.PermitNumber,
        Title = p.Title,
        Type = p.Type,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        Location = p.Location,
        PlannedStart = p.PlannedStart,
        PlannedEnd = p.PlannedEnd,
        RequestedBy = p.RequestedBy.FullName,
        Company = p.RequestedBy.Company,
        IsOverdue = p.IsOverdue(utcNow)
    };

    public static PermitDetailDto ToDetailDto(this Permit p, int userId, UserRole role, DateTime utcNow) => new()
    {
        Id = p.Id,
        PermitNumber = p.PermitNumber,
        Title = p.Title,
        Type = p.Type,
        Status = p.Status,
        RiskLevel = p.RiskLevel,
        Location = p.Location,
        PlannedStart = p.PlannedStart,
        PlannedEnd = p.PlannedEnd,
        RequestedBy = p.RequestedBy.FullName,
        Company = p.RequestedBy.Company,
        IsOverdue = p.IsOverdue(utcNow),
        WorkDescription = p.WorkDescription,
        EquipmentTag = p.EquipmentTag,
        WorkersCount = p.WorkersCount,
        Hazards = p.Hazards,
        Precautions = p.Precautions,
        RequiredPpe = p.RequiredPpe,
        RequestedById = p.RequestedById,
        ApprovedBy = p.ApprovedBy?.FullName,
        IssuedBy = p.IssuedBy?.FullName,
        CreatedAt = p.CreatedAt,
        SubmittedAt = p.SubmittedAt,
        ApprovedAt = p.ApprovedAt,
        IssuedAt = p.IssuedAt,
        ClosedAt = p.ClosedAt,
        LastComment = p.LastComment,
        RequiresGasTest = p.RequiresGasTest,
        RequiresIsolation = p.RequiresIsolation,
        AvailableActions = PermitWorkflow.AvailableActions(p, role, userId),
        CanEdit = p.Status == PermitStatus.Draft && (p.RequestedById == userId || role == UserRole.Admin),
        CanRecordSafetyChecks = PermitService.CanRecordSafetyChecks(p, role),
        GasTests = p.GasTests.OrderByDescending(g => g.TestedAt).Select(g => g.ToDto(p.Type)).ToList(),
        Isolations = p.Isolations.OrderBy(i => i.AppliedAt).Select(i => i.ToDto()).ToList(),
        History = p.History.OrderByDescending(h => h.Timestamp).Select(h => new PermitHistoryDto
        {
            Action = h.Action,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            Comment = h.Comment,
            PerformedBy = h.PerformedBy.FullName,
            Timestamp = h.Timestamp
        }).ToList()
    };

    public static GasTestDto ToDto(this GasTest g, PermitType permitType) => new()
    {
        Id = g.Id,
        OxygenPercent = g.OxygenPercent,
        LelPercent = g.LelPercent,
        H2sPpm = g.H2sPpm,
        CoPpm = g.CoPpm,
        IsPassed = g.IsPassed,
        Remarks = g.Remarks,
        TestedBy = g.TestedBy.FullName,
        TestedAt = g.TestedAt,
        Failures = GasTestEvaluator.Evaluate(permitType, g.OxygenPercent, g.LelPercent, g.H2sPpm, g.CoPpm).Failures
    };

    public static IsolationDto ToDto(this Isolation i) => new()
    {
        Id = i.Id,
        Type = i.Type,
        IsolationPoint = i.IsolationPoint,
        LockNumber = i.LockNumber,
        Description = i.Description,
        IsRemoved = i.IsRemoved,
        AppliedBy = i.AppliedBy.FullName,
        AppliedAt = i.AppliedAt,
        RemovedBy = i.RemovedBy?.FullName,
        RemovedAt = i.RemovedAt
    };
}
