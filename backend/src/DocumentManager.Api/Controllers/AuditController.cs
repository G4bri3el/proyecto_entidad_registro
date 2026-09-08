using DocumentManager.Application.Audit;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManager.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanViewAudit)]
[Route("api/audit")]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        return Ok(await auditService.SearchAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await auditService.GetAsync(id, cancellationToken));
    }
}
