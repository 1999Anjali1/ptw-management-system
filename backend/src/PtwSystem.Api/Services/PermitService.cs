using Microsoft.EntityFrameworkCore;
using PtwSystem.Api.Common;
using PtwSystem.Api.Data;
using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Domain.Enums;
using PtwSystem.Api.Domain.Rules;
using PtwSystem.Api.Dtos;

namespace PtwSystem.Api.Services;

public interface IPermitService
{
    Task<PagedResult<PermitSummaryDto>> GetPagedAsync(PermitQuery query);
    Task<PermitDetailDto> GetByIdAsync(int id);
    Task<PermitDetailDto> CreateAsync(PermitRequest request);
    Task<PermitDetailDto> UpdateAsync(int id, PermitRequest request);
    Task<PermitDetailDto> PerformActionAsync(int id, PermitActionRequest request);
    Task<PermitDetailDto> AddGasTestAsync(int id, GasTestRequest request);
    Task<PermitDetailDto> AddIsolationAsync(int id, IsolationRequest request);
    Task<PermitDetailDto> RemoveIsolationAsync(int id, int isolationId);
    Task<IReadOnlyList<PermitConflictDto>> GetConflictsAsync(int id);
}

public class PermitService(AppDbContext db, ICurrentUser currentUser, TimeProvider clock) : IPermitService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<PermitSummaryDto>> GetPagedAsync(PermitQuery query)
    {
        var q = db.Permits.AsNoTracking().Include(p => p.RequestedBy).AsQueryable();

        if (query.Status.HasValue) q = q.Where(p => p.Status == query.Status.Value);
        if (query.Type.HasValue) q = q.Where(p => p.Type == query.Type.Value);
        if (query.Mine) q = q.Where(p => p.RequestedById == currentUser.Id);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(p => p.Title.ToLower().Contains(s)
                          || p.PermitNumber.ToLower().Contains(s)
                          || p.Location.ToLower().Contains(s)
                          || (p.EquipmentTag != null && p.EquipmentTag.ToLower().Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var now = Now;
        return new PagedResult<PermitSummaryDto>
        {
            Items = items.Select(p => p.ToSummaryDto(now)).ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<PermitDetailDto> GetByIdAsync(int id)
    {
        var permit = await LoadAsync(id, tracking: false);
        return ToDetail(permit);
    }

    public async Task<PermitDetailDto> CreateAsync(PermitRequest request)
    {
        if (currentUser.Role is not (UserRole.Requester or UserRole.Admin))
            throw new ForbiddenException("Only requesters can raise a new permit.");

        ValidateDates(request);

        var permit = new Permit
        {
            PermitNumber = await NextPermitNumberAsync(),
            RequestedById = currentUser.Id,
            Status = PermitStatus.Draft,
            CreatedAt = Now
        };
        Apply(request, permit);
        permit.History.Add(NewHistory("Created", null, PermitStatus.Draft));

        db.Permits.Add(permit);
        await db.SaveChangesAsync();
        return await GetByIdAsync(permit.Id);
    }

    public async Task<PermitDetailDto> UpdateAsync(int id, PermitRequest request)
    {
        var permit = await LoadAsync(id);

        if (permit.Status != PermitStatus.Draft)
            throw new BusinessRuleException("Only draft permits can be edited.");
        if (permit.RequestedById != currentUser.Id && currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("You can only edit your own permits.");

        ValidateDates(request);
        Apply(request, permit);
        permit.History.Add(NewHistory("Edited", PermitStatus.Draft, PermitStatus.Draft));

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PermitDetailDto> PerformActionAsync(int id, PermitActionRequest request)
    {
        var permit = await LoadAsync(id);
        var now = Now;

        var errors = PermitWorkflow.Validate(permit, request.Action, currentUser.Role, currentUser.Id, request.Comment, now);
        if (errors.Count > 0) throw new BusinessRuleException(errors);

        var from = permit.Status;
        var to = PermitWorkflow.TargetStatus(request.Action);
        permit.Status = to;

        switch (request.Action)
        {
            case PermitAction.Submit:
                permit.SubmittedAt = now;
                break;
            case PermitAction.Approve:
                permit.ApprovedAt = now;
                permit.ApprovedById = currentUser.Id;
                break;
            case PermitAction.Issue:
                permit.IssuedAt = now;
                permit.IssuedById = currentUser.Id;
                break;
            case PermitAction.Close:
                permit.ClosedAt = now;
                break;
        }

        if (!string.IsNullOrWhiteSpace(request.Comment))
            permit.LastComment = request.Comment.Trim();

        permit.History.Add(NewHistory(PastTense(request.Action), from, to, request.Comment));
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PermitDetailDto> AddGasTestAsync(int id, GasTestRequest request)
    {
        var permit = await LoadAsync(id);
        EnsureCanRecordSafetyChecks(permit);

        var result = GasTestEvaluator.Evaluate(permit.Type, request.OxygenPercent, request.LelPercent, request.H2sPpm, request.CoPpm);

        permit.GasTests.Add(new GasTest
        {
            OxygenPercent = request.OxygenPercent,
            LelPercent = request.LelPercent,
            H2sPpm = request.H2sPpm,
            CoPpm = request.CoPpm,
            IsPassed = result.IsPassed,
            Remarks = request.Remarks?.Trim(),
            TestedById = currentUser.Id,
            TestedAt = Now
        });

        var summary = $"O2 {request.OxygenPercent}%, LEL {request.LelPercent}%, H2S {request.H2sPpm} ppm, CO {request.CoPpm} ppm";
        permit.History.Add(NewHistory(result.IsPassed ? "Gas test PASSED" : "Gas test FAILED", permit.Status, permit.Status, summary));

        // Safety interlock: a failed gas test on a live job suspends the permit immediately.
        if (!result.IsPassed && permit.Status == PermitStatus.Active)
        {
            permit.Status = PermitStatus.Suspended;
            permit.LastComment = "Automatically suspended - gas test failed. " + string.Join(" ", result.Failures);
            permit.History.Add(NewHistory("Suspended", PermitStatus.Active, PermitStatus.Suspended, permit.LastComment));
        }

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PermitDetailDto> AddIsolationAsync(int id, IsolationRequest request)
    {
        var permit = await LoadAsync(id);
        EnsureCanRecordSafetyChecks(permit);

        permit.Isolations.Add(new Isolation
        {
            Type = request.Type,
            IsolationPoint = request.IsolationPoint.Trim(),
            LockNumber = request.LockNumber?.Trim(),
            Description = request.Description?.Trim(),
            AppliedById = currentUser.Id,
            AppliedAt = Now
        });
        permit.History.Add(NewHistory("Isolation applied", permit.Status, permit.Status,
            $"{request.Type}: {request.IsolationPoint} (lock {request.LockNumber ?? "n/a"})"));

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<PermitDetailDto> RemoveIsolationAsync(int id, int isolationId)
    {
        var permit = await LoadAsync(id);
        EnsureCanRecordSafetyChecks(permit);

        var isolation = permit.Isolations.FirstOrDefault(i => i.Id == isolationId)
            ?? throw new NotFoundException("Isolation not found on this permit.");
        if (isolation.IsRemoved)
            throw new BusinessRuleException("Isolation has already been removed.");
        if (permit.Status == PermitStatus.Active)
            throw new BusinessRuleException("Suspend the permit (stop work) before removing isolations.");

        isolation.IsRemoved = true;
        isolation.RemovedById = currentUser.Id;
        isolation.RemovedAt = Now;
        permit.History.Add(NewHistory("Isolation removed", permit.Status, permit.Status,
            $"{isolation.Type}: {isolation.IsolationPoint}"));

        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<PermitConflictDto>> GetConflictsAsync(int id)
    {
        var permit = await db.Permits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Permit {id} not found.");

        var location = permit.Location.ToLower();

        return await db.Permits.AsNoTracking()
            .Where(p => p.Id != id
                        // "live" permits on site
                        && (p.Status == PermitStatus.Approved || p.Status == PermitStatus.Active || p.Status == PermitStatus.Suspended)
                        && p.Location.ToLower() == location
                        && p.PlannedStart < permit.PlannedEnd
                        && p.PlannedEnd > permit.PlannedStart)
            .OrderBy(p => p.PlannedStart)
            .Select(p => new PermitConflictDto
            {
                Id = p.Id,
                PermitNumber = p.PermitNumber,
                Title = p.Title,
                Type = p.Type,
                Status = p.Status,
                PlannedStart = p.PlannedStart,
                PlannedEnd = p.PlannedEnd
            })
            .ToListAsync();
    }

    /// <summary>Area Authority, Issuer or Admin can record gas tests / isolations on a live permit.</summary>
    public static bool CanRecordSafetyChecks(Permit permit, UserRole role) =>
        (role is UserRole.AreaAuthority or UserRole.Issuer or UserRole.Admin)
        && (permit.Status is PermitStatus.Approved or PermitStatus.Active or PermitStatus.Suspended);

    // ---------- helpers ----------

    private void EnsureCanRecordSafetyChecks(Permit permit)
    {
        if (currentUser.Role is not (UserRole.AreaAuthority or UserRole.Issuer or UserRole.Admin))
            throw new ForbiddenException("Only Area Authority or Issuer can record gas tests and isolations.");
        if (permit.Status is not (PermitStatus.Approved or PermitStatus.Active or PermitStatus.Suspended))
            throw new BusinessRuleException($"Safety checks cannot be recorded on a {permit.Status} permit.");
    }

    private async Task<Permit> LoadAsync(int id, bool tracking = true)
    {
        var q = db.Permits
            .Include(p => p.RequestedBy)
            .Include(p => p.ApprovedBy)
            .Include(p => p.IssuedBy)
            .Include(p => p.GasTests).ThenInclude(g => g.TestedBy)
            .Include(p => p.Isolations).ThenInclude(i => i.AppliedBy)
            .Include(p => p.Isolations).ThenInclude(i => i.RemovedBy)
            .Include(p => p.History).ThenInclude(h => h.PerformedBy)
            .AsSplitQuery();

        if (!tracking) q = q.AsNoTracking();

        return await q.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Permit {id} not found.");
    }

    private PermitDetailDto ToDetail(Permit permit) => permit.ToDetailDto(currentUser.Id, currentUser.Role, Now);

    private PermitHistory NewHistory(string action, PermitStatus? from, PermitStatus to, string? comment = null) => new()
    {
        Action = action,
        FromStatus = from,
        ToStatus = to,
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
        PerformedById = currentUser.Id,
        Timestamp = Now
    };

    private static void Apply(PermitRequest r, Permit p)
    {
        p.Type = r.Type;
        p.RiskLevel = r.RiskLevel;
        p.Title = r.Title.Trim();
        p.WorkDescription = r.WorkDescription.Trim();
        p.Location = r.Location.Trim();
        p.EquipmentTag = string.IsNullOrWhiteSpace(r.EquipmentTag) ? null : r.EquipmentTag.Trim();
        p.WorkersCount = r.WorkersCount;
        p.PlannedStart = ToUtc(r.PlannedStart);
        p.PlannedEnd = ToUtc(r.PlannedEnd);
        p.Hazards = r.Hazards.Trim();
        p.Precautions = r.Precautions.Trim();
        p.RequiredPpe = r.RequiredPpe.Trim();
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static void ValidateDates(PermitRequest r)
    {
        if (r.PlannedEnd <= r.PlannedStart)
            throw new BusinessRuleException("Planned end must be after planned start.");
        if ((r.PlannedEnd - r.PlannedStart).TotalHours > 24)
            throw new BusinessRuleException("A permit cannot be valid for more than 24 hours. Raise a new permit for the next shift.");
    }

    private async Task<string> NextPermitNumberAsync()
    {
        var prefix = $"PTW-{Now.Year}-";
        var last = await db.Permits
            .Where(p => p.PermitNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PermitNumber)
            .Select(p => p.PermitNumber)
            .FirstOrDefaultAsync();

        var next = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;
        return $"{prefix}{next:D4}";
    }

    private static string PastTense(PermitAction action) => action switch
    {
        PermitAction.Submit => "Submitted",
        PermitAction.Approve => "Approved",
        PermitAction.Reject => "Rejected",
        PermitAction.Issue => "Issued",
        PermitAction.Suspend => "Suspended",
        PermitAction.Resume => "Resumed",
        PermitAction.Close => "Closed",
        PermitAction.Cancel => "Cancelled",
        _ => action.ToString()
    };
}
