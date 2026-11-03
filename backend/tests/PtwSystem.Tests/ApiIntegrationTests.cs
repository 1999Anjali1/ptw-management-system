using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PtwSystem.Api.Domain.Enums;
using PtwSystem.Api.Dtos;

namespace PtwSystem.Tests;

/// <summary>Boots the real API in memory (with its own SQLite file) and calls it over HTTP.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"ptw-tests-{Guid.NewGuid():N}.db");

        // Read by WebApplication.CreateBuilder, so it applies before the DbContext is registered
        Environment.SetEnvironmentVariable("ConnectionStrings__Sqlite", $"Data Source={dbPath}");
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
    }
}

public class ApiIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<HttpClient> ClientFor(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "Password@123" }, Json);
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "issuer@ptwdemo.com", Password = "wrong" }, Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Permits_require_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/permits");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_permits_are_listed()
    {
        var client = await ClientFor("issuer@ptwdemo.com");

        var page = await client.GetFromJsonAsync<PagedResult<PermitSummaryDto>>("/api/permits?pageSize=50", Json);

        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 8);
    }

    [Fact]
    public async Task Swagger_document_is_available()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Hot_work_full_lifecycle_enforces_gas_test()
    {
        var requester = await ClientFor("requester@ptwdemo.com");
        var areaAuthority = await ClientFor("areaauthority@ptwdemo.com");
        var issuer = await ClientFor("issuer@ptwdemo.com");

        // 1. Requester raises and submits a hot work permit
        var create = await requester.PostAsJsonAsync("/api/permits", new PermitRequest
        {
            Type = PermitType.HotWork,
            RiskLevel = RiskLevel.High,
            Title = "Integration test welding job",
            WorkDescription = "Weld repair on support bracket",
            Location = "Test Area 1",
            WorkersCount = 2,
            PlannedStart = DateTime.UtcNow.AddMinutes(10),
            PlannedEnd = DateTime.UtcNow.AddHours(6),
            Hazards = "Fire",
            Precautions = "Fire watch",
            RequiredPpe = "Welding PPE"
        }, Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var permit = (await create.Content.ReadFromJsonAsync<PermitDetailDto>(Json))!;
        Assert.Equal(PermitStatus.Draft, permit.Status);
        Assert.StartsWith("PTW-", permit.PermitNumber);

        permit = await Act(requester, permit.Id, PermitAction.Submit);
        Assert.Equal(PermitStatus.Submitted, permit.Status);

        // 2. Requester is not allowed to approve
        var selfApprove = await requester.PostAsJsonAsync($"/api/permits/{permit.Id}/actions",
            new PermitActionRequest { Action = PermitAction.Approve }, Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, selfApprove.StatusCode);

        // 3. Area Authority approves
        permit = await Act(areaAuthority, permit.Id, PermitAction.Approve);
        Assert.Equal(PermitStatus.Approved, permit.Status);

        // 4. Issue is blocked without a gas test
        var blocked = await issuer.PostAsJsonAsync($"/api/permits/{permit.Id}/actions",
            new PermitActionRequest { Action = PermitAction.Issue }, Json);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);

        // 5. Passing gas test, then issue succeeds
        var gas = await issuer.PostAsJsonAsync($"/api/permits/{permit.Id}/gas-tests",
            new GasTestRequest { OxygenPercent = 20.9, LelPercent = 0, H2sPpm = 0, CoPpm = 0 }, Json);
        gas.EnsureSuccessStatusCode();

        permit = await Act(issuer, permit.Id, PermitAction.Issue);
        Assert.Equal(PermitStatus.Active, permit.Status);

        // 6. A failed gas test on an active permit suspends it automatically
        var failed = await issuer.PostAsJsonAsync($"/api/permits/{permit.Id}/gas-tests",
            new GasTestRequest { OxygenPercent = 20.9, LelPercent = 12, H2sPpm = 0, CoPpm = 0 }, Json);
        permit = (await failed.Content.ReadFromJsonAsync<PermitDetailDto>(Json))!;
        Assert.Equal(PermitStatus.Suspended, permit.Status);

        // 7. Close out
        permit = await Act(issuer, permit.Id, PermitAction.Close);
        Assert.Equal(PermitStatus.Closed, permit.Status);
        Assert.True(permit.History.Count >= 7);
    }

    private static async Task<PermitDetailDto> Act(HttpClient client, int id, PermitAction action, string? comment = null)
    {
        var response = await client.PostAsJsonAsync($"/api/permits/{id}/actions",
            new PermitActionRequest { Action = action, Comment = comment }, Json);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{action} failed: {body}");
        return JsonSerializer.Deserialize<PermitDetailDto>(body, Json)!;
    }
}
