namespace PtwSystem.Api.Domain.Entities;

/// <summary>Atmospheric test reading taken by an authorized gas tester.</summary>
public class GasTest
{
    public int Id { get; set; }

    public int PermitId { get; set; }
    public Permit Permit { get; set; } = null!;

    /// <summary>Oxygen, % by volume.</summary>
    public double OxygenPercent { get; set; }

    /// <summary>Flammable gas, % of Lower Explosive Limit.</summary>
    public double LelPercent { get; set; }

    /// <summary>Hydrogen sulphide, ppm.</summary>
    public double H2sPpm { get; set; }

    /// <summary>Carbon monoxide, ppm.</summary>
    public double CoPpm { get; set; }

    public bool IsPassed { get; set; }
    public string? Remarks { get; set; }

    public int TestedById { get; set; }
    public User TestedBy { get; set; } = null!;
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}
