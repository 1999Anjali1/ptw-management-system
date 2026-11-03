using Microsoft.EntityFrameworkCore;
using PtwSystem.Api.Data;
using PtwSystem.Api.Domain.Enums;
using PtwSystem.Api.Dtos;

namespace PtwSystem.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}

public class DashboardService(AppDbContext db, TimeProvider clock) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var now = clock.GetUtcNow().UtcDateTime;

        // Small projection - only the columns we need for counting
        var permits = await db.Permits.AsNoTracking()
            .Select(p => new { p.Status, p.Type, p.PlannedEnd })
            .ToListAsync();

        var openIsolations = await db.Isolations.CountAsync(i => !i.IsRemoved);

        var activeNow = await db.Permits.AsNoTracking()
            .Include(p => p.RequestedBy)
            .Where(p => p.Status == PermitStatus.Active || p.Status == PermitStatus.Suspended)
            .OrderBy(p => p.PlannedEnd)
            .Take(10)
            .ToListAsync();

        return new DashboardSummaryDto
        {
            TotalPermits = permits.Count,
            ActivePermits = permits.Count(p => p.Status == PermitStatus.Active),
            PendingApproval = permits.Count(p => p.Status == PermitStatus.Submitted),
            ReadyToIssue = permits.Count(p => p.Status == PermitStatus.Approved),
            Suspended = permits.Count(p => p.Status == PermitStatus.Suspended),
            Overdue = permits.Count(p => (p.Status is PermitStatus.Active or PermitStatus.Suspended) && p.PlannedEnd < now),
            OpenIsolations = openIsolations,
            ByStatus = Enum.GetValues<PermitStatus>().ToDictionary(s => s.ToString(), s => permits.Count(p => p.Status == s)),
            ByType = Enum.GetValues<PermitType>().ToDictionary(t => t.ToString(), t => permits.Count(p => p.Type == t)),
            ActiveNow = activeNow.Select(p => p.ToSummaryDto(now)).ToList()
        };
    }
}
