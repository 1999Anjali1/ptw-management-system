using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Domain.Rules;

/// <summary>
/// The permit state machine. Pure C# with no database access, so it is easy to unit test.
///
///   Draft ──Submit──► Submitted ──Approve──► Approved ──Issue──► Active ──Close──► Closed
///     │                  │                     │                 │  ▲
///     └──Cancel          ├──Reject             ├──Reject         Suspend│Resume
///                        └──Cancel             └──Cancel         ▼  │
///                                                              Suspended ──Close──► Closed
/// </summary>
public static class PermitWorkflow
{
    private sealed record Transition(
        PermitStatus[] From,
        PermitStatus To,
        UserRole[] Roles,
        bool OwnerOnly = false,
        bool NotOwner = false,
        bool CommentRequired = false);

    private static readonly Dictionary<PermitAction, Transition> Transitions = new()
    {
        [PermitAction.Submit] = new([PermitStatus.Draft], PermitStatus.Submitted,
            [UserRole.Requester, UserRole.Admin], OwnerOnly: true),

        [PermitAction.Approve] = new([PermitStatus.Submitted], PermitStatus.Approved,
            [UserRole.AreaAuthority, UserRole.Admin], NotOwner: true),

        [PermitAction.Reject] = new([PermitStatus.Submitted, PermitStatus.Approved], PermitStatus.Rejected,
            [UserRole.AreaAuthority, UserRole.Issuer, UserRole.Admin], NotOwner: true, CommentRequired: true),

        [PermitAction.Issue] = new([PermitStatus.Approved], PermitStatus.Active,
            [UserRole.Issuer, UserRole.Admin], NotOwner: true),

        [PermitAction.Suspend] = new([PermitStatus.Active], PermitStatus.Suspended,
            [UserRole.AreaAuthority, UserRole.Issuer, UserRole.Admin], CommentRequired: true),

        [PermitAction.Resume] = new([PermitStatus.Suspended], PermitStatus.Active,
            [UserRole.Issuer, UserRole.Admin]),

        [PermitAction.Close] = new([PermitStatus.Active, PermitStatus.Suspended], PermitStatus.Closed,
            [UserRole.Issuer, UserRole.Admin]),

        [PermitAction.Cancel] = new([PermitStatus.Draft, PermitStatus.Submitted, PermitStatus.Approved], PermitStatus.Cancelled,
            [UserRole.Requester, UserRole.Admin], OwnerOnly: true),
    };

    public static PermitStatus TargetStatus(PermitAction action) => Transitions[action].To;

    /// <summary>
    /// Is the action available for this user at the current status?
    /// Checks status, role and segregation of duties only - used to decide which buttons the UI shows.
    /// </summary>
    public static bool IsAvailable(Permit permit, PermitAction action, UserRole role, int userId)
    {
        var t = Transitions[action];
        if (!t.From.Contains(permit.Status)) return false;
        if (!t.Roles.Contains(role)) return false;

        var isOwner = permit.RequestedById == userId;
        if (role != UserRole.Admin)
        {
            if (t.OwnerOnly && !isOwner) return false;
            // Segregation of duties: you cannot approve or issue your own permit.
            if (t.NotOwner && isOwner) return false;
        }
        return true;
    }

    public static IReadOnlyList<PermitAction> AvailableActions(Permit permit, UserRole role, int userId) =>
        Enum.GetValues<PermitAction>().Where(a => IsAvailable(permit, a, role, userId)).ToList();

    /// <summary>
    /// Full validation: availability plus safety pre-conditions (gas test, isolations, dates).
    /// Returns a list of reasons the action is blocked; empty list means it can proceed.
    /// </summary>
    public static IReadOnlyList<string> Validate(
        Permit permit, PermitAction action, UserRole role, int userId, string? comment, DateTime utcNow)
    {
        var errors = new List<string>();
        var t = Transitions[action];

        if (!t.From.Contains(permit.Status))
        {
            errors.Add($"Cannot {action} a permit that is {permit.Status}.");
            return errors;
        }

        if (!IsAvailable(permit, action, role, userId))
        {
            errors.Add(t.NotOwner && permit.RequestedById == userId
                ? $"Segregation of duties: you cannot {action.ToString().ToLower()} your own permit."
                : $"Your role ({role}) is not allowed to {action.ToString().ToLower()} this permit.");
            return errors;
        }

        if (t.CommentRequired && string.IsNullOrWhiteSpace(comment))
            errors.Add($"A comment is required to {action.ToString().ToLower()} a permit.");

        switch (action)
        {
            case PermitAction.Submit:
                if (permit.PlannedEnd <= permit.PlannedStart)
                    errors.Add("Planned end must be after planned start.");
                if (permit.PlannedEnd <= utcNow)
                    errors.Add("Planned end is already in the past.");
                break;

            case PermitAction.Issue:
                if (permit.PlannedEnd <= utcNow)
                    errors.Add("Permit validity window has expired - raise a new permit.");
                AddSafetyChecks(permit, utcNow, errors);
                break;

            case PermitAction.Resume:
                AddSafetyChecks(permit, utcNow, errors);
                break;

            case PermitAction.Close:
                var open = permit.Isolations.Count(i => !i.IsRemoved);
                if (open > 0)
                    errors.Add($"{open} isolation(s) still applied - remove all isolations before closing.");
                break;
        }

        return errors;
    }

    private static void AddSafetyChecks(Permit permit, DateTime utcNow, List<string> errors)
    {
        if (permit.RequiresGasTest)
        {
            var latest = permit.GasTests.OrderByDescending(g => g.TestedAt).FirstOrDefault();
            if (latest is null)
                errors.Add($"{permit.Type} requires a gas test before work can start.");
            else if (!latest.IsPassed)
                errors.Add("Latest gas test FAILED - retest required.");
            else if (utcNow - latest.TestedAt > GasTestEvaluator.Validity)
                errors.Add($"Latest gas test is older than {GasTestEvaluator.Validity.TotalHours} hours - retest required.");
        }

        if (permit.RequiresIsolation && !permit.Isolations.Any(i => !i.IsRemoved))
            errors.Add($"{permit.Type} requires at least one energy isolation to be applied.");
    }
}
