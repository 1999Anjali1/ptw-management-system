using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtwSystem.Api.Dtos;
using PtwSystem.Api.Services;

namespace PtwSystem.Api.Controllers;

[ApiController]
[Route("api/permits")]
[Authorize]
public class PermitsController(IPermitService permits) : ControllerBase
{
    /// <summary>Search and filter permits (paged).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PermitSummaryDto>>> GetAll([FromQuery] PermitQuery query) =>
        Ok(await permits.GetPagedAsync(query));

    /// <summary>Full permit details including gas tests, isolations, audit history and allowed actions.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermitDetailDto>> GetById(int id) => Ok(await permits.GetByIdAsync(id));

    /// <summary>Raise a new permit (saved as Draft).</summary>
    [HttpPost]
    [ProducesResponseType<PermitDetailDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PermitDetailDto>> Create(PermitRequest request)
    {
        var created = await permits.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Edit a draft permit.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PermitDetailDto>> Update(int id, PermitRequest request) =>
        Ok(await permits.UpdateAsync(id, request));

    /// <summary>
    /// Move the permit through its workflow: Submit, Approve, Reject, Issue, Suspend, Resume, Close, Cancel.
    /// Returns 422 with the list of reasons when a PTW rule blocks the action.
    /// </summary>
    [HttpPost("{id:int}/actions")]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PermitDetailDto>> PerformAction(int id, PermitActionRequest request) =>
        Ok(await permits.PerformActionAsync(id, request));

    /// <summary>Record a gas test. A failed test on an active permit suspends it automatically.</summary>
    [HttpPost("{id:int}/gas-tests")]
    public async Task<ActionResult<PermitDetailDto>> AddGasTest(int id, GasTestRequest request) =>
        Ok(await permits.AddGasTestAsync(id, request));

    /// <summary>Apply an energy isolation (lock-out / tag-out).</summary>
    [HttpPost("{id:int}/isolations")]
    public async Task<ActionResult<PermitDetailDto>> AddIsolation(int id, IsolationRequest request) =>
        Ok(await permits.AddIsolationAsync(id, request));

    /// <summary>Remove (de-isolate) an isolation. The permit must not be Active.</summary>
    [HttpPost("{id:int}/isolations/{isolationId:int}/remove")]
    public async Task<ActionResult<PermitDetailDto>> RemoveIsolation(int id, int isolationId) =>
        Ok(await permits.RemoveIsolationAsync(id, isolationId));

    /// <summary>SIMOPS check: other live permits at the same location with overlapping times.</summary>
    [HttpGet("{id:int}/conflicts")]
    public async Task<ActionResult<IReadOnlyList<PermitConflictDto>>> GetConflicts(int id) =>
        Ok(await permits.GetConflictsAsync(id));
}
