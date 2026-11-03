using PtwSystem.Api.Domain.Enums;
using PtwSystem.Api.Domain.Rules;

namespace PtwSystem.Tests;

public class GasTestEvaluatorTests
{
    [Fact]
    public void Normal_air_passes_for_hot_work()
    {
        var result = GasTestEvaluator.Evaluate(PermitType.HotWork, oxygen: 20.9, lel: 0, h2s: 0, co: 0);

        Assert.True(result.IsPassed);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void Any_LEL_fails_hot_work()
    {
        var result = GasTestEvaluator.Evaluate(PermitType.HotWork, 20.9, lel: 1, h2s: 0, co: 0);

        Assert.False(result.IsPassed);
        Assert.Contains(result.Failures, f => f.Contains("LEL"));
    }

    [Fact]
    public void Low_LEL_is_acceptable_for_confined_space_entry()
    {
        var result = GasTestEvaluator.Evaluate(PermitType.ConfinedSpaceEntry, 20.9, lel: 5, h2s: 0, co: 0);

        Assert.True(result.IsPassed);
    }

    [Theory]
    [InlineData(19.4)]
    [InlineData(23.6)]
    public void Oxygen_outside_safe_range_fails(double oxygen)
    {
        var result = GasTestEvaluator.Evaluate(PermitType.ConfinedSpaceEntry, oxygen, 0, 0, 0);

        Assert.False(result.IsPassed);
        Assert.Contains(result.Failures, f => f.Contains("Oxygen"));
    }

    [Fact]
    public void Toxic_gases_above_limit_report_every_failure()
    {
        var result = GasTestEvaluator.Evaluate(PermitType.ConfinedSpaceEntry, 20.9, 0, h2s: 15, co: 40);

        Assert.False(result.IsPassed);
        Assert.Equal(2, result.Failures.Count);
    }
}
