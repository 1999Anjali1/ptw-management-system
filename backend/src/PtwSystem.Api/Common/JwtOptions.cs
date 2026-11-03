namespace PtwSystem.Api.Common;

/// <summary>Bound from the "Jwt" section of appsettings.json.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC signing key - at least 32 characters. Use user-secrets or env vars outside development.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpiryHours { get; set; } = 8;
}
