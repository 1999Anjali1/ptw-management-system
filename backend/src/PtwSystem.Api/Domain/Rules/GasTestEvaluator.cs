using PtwSystem.Api.Domain.Enums;

namespace PtwSystem.Api.Domain.Rules;

/// <summary>
/// Evaluates gas test readings against acceptance limits.
/// Limits are typical oil &amp; gas site values - adjust to your site's PTW procedure.
/// </summary>
public static class GasTestEvaluator
{
    public const double MinOxygen = 19.5;
    public const double MaxOxygen = 23.5;

    /// <summary>Hot work requires a flammable-free atmosphere.</summary>
    public const double MaxLelHotWork = 0;

    /// <summary>General limit for other work (e.g. confined space entry).</summary>
    public const double MaxLelGeneral = 10;

    public const double MaxH2sPpm = 10;
    public const double MaxCoPpm = 25;

    /// <summary>A passed gas test is valid for this long before the permit can be issued / resumed.</summary>
    public static readonly TimeSpan Validity = TimeSpan.FromHours(2);

    public static GasTestResult Evaluate(PermitType permitType, double oxygen, double lel, double h2s, double co)
    {
        var failures = new List<string>();

        if (oxygen < MinOxygen || oxygen > MaxOxygen)
            failures.Add($"Oxygen {oxygen}% is outside the safe range {MinOxygen}-{MaxOxygen}%.");

        var maxLel = permitType == PermitType.HotWork ? MaxLelHotWork : MaxLelGeneral;
        if (permitType == PermitType.HotWork ? lel > maxLel : lel >= maxLel)
            failures.Add(permitType == PermitType.HotWork
                ? $"LEL {lel}% - hot work requires 0% LEL."
                : $"LEL {lel}% must be below {maxLel}%.");

        if (h2s >= MaxH2sPpm)
            failures.Add($"H2S {h2s} ppm must be below {MaxH2sPpm} ppm.");

        if (co >= MaxCoPpm)
            failures.Add($"CO {co} ppm must be below {MaxCoPpm} ppm.");

        return new GasTestResult(failures.Count == 0, failures);
    }
}

public record GasTestResult(bool IsPassed, IReadOnlyList<string> Failures);
