using DocumentManager.Application.Common;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManager.Api.Controllers;

[ApiController]
[Authorize(Policy = AppPolicies.CanRestoreDocuments)]
[Route("api/trash")]
public sealed class TrashController(ITrashService trashService) : ControllerBase
{
    [HttpGet("documents")]
    public async Task<IActionResult> Documents([FromQuery] PageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trashService.GetDeletedDocumentsAsync(request, cancellationToken));
    }

    [HttpGet("folders")]
    public async Task<IActionResult> Folders([FromQuery] PageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await trashService.GetDeletedFoldersAsync(request, cancellationToken));
    }
}
