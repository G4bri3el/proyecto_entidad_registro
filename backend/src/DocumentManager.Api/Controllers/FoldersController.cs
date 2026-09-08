using DocumentManager.Application.Folders;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/folders")]
public sealed class FoldersController(IFolderService folderService) : ControllerBase
{
    [HttpGet("tree")]
    [Authorize(Policy = AppPolicies.CanViewDocuments)]
    public async Task<IActionResult> Tree(CancellationToken cancellationToken)
    {
        return Ok(await folderService.GetTreeAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPolicies.CanViewDocuments)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await folderService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CanCreateFolders)]
    public async Task<IActionResult> Create(CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var folder = await folderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = folder.Id }, folder);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPolicies.CanRenameFolders)]
    public async Task<IActionResult> Rename(Guid id, UpdateFolderRequest request, CancellationToken cancellationToken)
    {
        return Ok(await folderService.RenameAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/move")]
    [Authorize(Policy = AppPolicies.CanMoveFolders)]
    public async Task<IActionResult> Move(Guid id, MoveFolderRequest request, CancellationToken cancellationToken)
    {
        return Ok(await folderService.MoveAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPolicies.CanDeleteFolders)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await folderService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
