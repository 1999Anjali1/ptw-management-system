namespace PtwSystem.Api.Domain.Enums;

/// <summary>Roles in the permit-to-work process.</summary>
public enum UserRole
{
    /// <summary>Performing authority / permit applicant (usually contractor supervisor).</summary>
    Requester = 1,

    /// <summary>Area Authority - owns the plant area and approves work in it.</summary>
    AreaAuthority = 2,

    /// <summary>Permit Issuer - verifies site conditions and issues the permit.</summary>
    Issuer = 3,

    /// <summary>System administrator.</summary>
    Admin = 4
}

public enum PermitType
{
    ColdWork = 1,
    HotWork = 2,
    ConfinedSpaceEntry = 3,
    Electrical = 4,
    WorkAtHeight = 5,
    Excavation = 6
}

/// <summary>
/// Permit lifecycle:
/// Draft → Submitted → Approved → Active ⇄ Suspended → Closed.
/// Rejected and Cancelled are terminal side exits.
/// </summary>
public enum PermitStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Active = 4,
    Suspended = 5,
    Closed = 6,
    Rejected = 7,
    Cancelled = 8
}

public enum PermitAction
{
    Submit = 1,
    Approve = 2,
    Reject = 3,
    Issue = 4,
    Suspend = 5,
    Resume = 6,
    Close = 7,
    Cancel = 8
}

public enum RiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public enum IsolationType
{
    Electrical = 1,
    Mechanical = 2,
    Process = 3,
    Instrument = 4
}
