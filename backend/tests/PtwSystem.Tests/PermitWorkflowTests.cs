using PtwSystem.Api.Domain.Entities;
using PtwSystem.Api.Domain.Enums;
using PtwSystem.Api.Domain.Rules;

namespace PtwSystem.Tests;

public class PermitWorkflowTests
{
    private const int RequesterId = 1;
    private const int AreaAuthorityId = 2;
    private const int IssuerId = 3;
    private static readonly DateTime Now = new(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);

    private static Permit NewPermit(PermitType type, PermitStatus status) => new()
    {
        Id = 1,
        Type = type,
        Status = status,
        RequestedById = RequesterId,
        PlannedStart = Now.AddHours(1),
        PlannedEnd = Now.AddHours(9)
    };

    [Fact]
    public void Requester_can_submit_own_draft()
    {
        var permit = NewPermit(PermitType.ColdWork, PermitStatus.Draft);

        var errors = PermitWorkflow.Validate(permit, PermitAction.Submit, UserRole.Requester, RequesterId, null, Now);

        Assert.Empty(errors);
    }

    [Fact]
    public void Requester_cannot_submit_someone_elses_draft()
    {
        var permit = NewPermit(PermitType.ColdWork, PermitStatus.Draft);

        Assert.False(PermitWorkflow.IsAvailable(permit, PermitAction.Submit, UserRole.Requester, userId: 99));
    }

    [Fact]
    public void Requester_cannot_approve()
    {
        var permit = NewPermit(PermitType.ColdWork, PermitStatus.Submitted);

        var errors = PermitWorkflow.Validate(permit, PermitAction.Approve, UserRole.Requester, 99, null, Now);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Segregation_of_duties_blocks_approving_own_permit()
    {
        var permit = NewPermit(PermitType.ColdWork, PermitStatus.Submitted);
        permit.RequestedById = AreaAuthorityId;

        var errors = PermitWorkflow.Validate(permit, PermitAction.Approve, UserRole.AreaAuthority, AreaAuthorityId, null, Now);

        Assert.Contains(errors, e => e.Contains("Segregation of duties"));
    }

    [Fact]
    public void Reject_requires_a_comment()
    {
        var permit = NewPermit(PermitType.ColdWork, PermitStatus.Submitted);

        var errors = PermitWorkflow.Validate(permit, PermitAction.Reject, UserRole.AreaAuthority, AreaAuthorityId, "  ", Now);

        Assert.Contains(errors, e => e.Contains("comment"));
    }

    [Fact]
    public void Hot_work_cannot_be_issued_without_gas_test()
    {
        var permit = NewPermit(PermitType.HotWork, PermitStatus.Approved);

        var errors = PermitWorkflow.Validate(permit, PermitAction.Issue, UserRole.Issuer, IssuerId, null, Now);

        Assert.Contains(errors, e => e.Contains("gas test"));
    }

    [Fact]
    public void Hot_work_can_be_issued_with_recent_passed_gas_test()
    {
        var permit = NewPermit(PermitType.HotWork, PermitStatus.Approved);
        permit.GasTests.Add(new GasTest { IsPassed = true, TestedAt = Now.AddMinutes(-30) });

        var errors = PermitWorkflow.Validate(permit, PermitAction.Issue, UserRole.Issuer, IssuerId, null, Now);

        Assert.Empty(errors);
    }

    [Fact]
    public void Gas_test_older_than_validity_requires_retest()
    {
        var permit = NewPermit(PermitType.HotWork, PermitStatus.Approved);
        permit.GasTests.Add(new GasTest { IsPassed = true, TestedAt = Now.AddHours(-3) });

        var errors = PermitWorkflow.Validate(permit, PermitAction.Issue, UserRole.Issuer, IssuerId, null, Now);

        Assert.Contains(errors, e => e.Contains("retest"));
    }

    [Fact]
    public void Latest_gas_test_is_the_one_that_counts()
    {
        var permit = NewPermit(PermitType.HotWork, PermitStatus.Approved);
        permit.GasTests.Add(new GasTest { IsPassed = true, TestedAt = Now.AddMinutes(-60) });
        permit.GasTests.Add(new GasTest { IsPassed = false, TestedAt = Now.AddMinutes(-10) });

        var errors = PermitWorkflow.Validate(permit, PermitAction.Issue, UserRole.Issuer, IssuerId, null, Now);

        Assert.Contains(errors, e => e.Contains("FAILED"));
    }

    [Fact]
    public void Electrical_work_requires_an_applied_isolation()
    {
        var permit = NewPermit(PermitType.Electrical, PermitStatus.Approved);
        permit.Isolations.Add(new Isolation { IsRemoved = true });

        var errors = PermitWorkflow.Validate(permit, PermitAction.Issue, UserRole.Issuer, IssuerId, null, Now);

        Assert.Contains(errors, e => e.Contains("isolation"));
    }

    [Fact]
    public void Cannot_close_while_isolations_are_applied()
    {
        var permit = NewPermit(PermitType.Electrical, PermitStatus.Active);
        permit.Isolations.Add(new Isolation { IsRemoved = false });

        var errors = PermitWorkflow.Validate(permit, PermitAction.Close, UserRole.Issuer, IssuerId, null, Now);

        Assert.Contains(errors, e => e.Contains("isolation"));
    }

    [Fact]
    public void Available_actions_for_issuer_on_active_permit()
    {
        var permit = NewPermit(PermitType.ColdWork, PermitStatus.Active);

        var actions = PermitWorkflow.AvailableActions(permit, UserRole.Issuer, IssuerId);

        Assert.Equal(new[] { PermitAction.Suspend, PermitAction.Close }, actions);
    }

    [Theory]
    [InlineData(PermitStatus.Closed)]
    [InlineData(PermitStatus.Rejected)]
    [InlineData(PermitStatus.Cancelled)]
    public void Terminal_statuses_have_no_actions(PermitStatus status)
    {
        var permit = NewPermit(PermitType.ColdWork, status);

        Assert.Empty(PermitWorkflow.AvailableActions(permit, UserRole.Admin, 999));
    }
}
