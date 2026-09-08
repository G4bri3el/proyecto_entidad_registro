using DocumentManager.Application.Documents;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManager.Api.Controllers;

[ApiController]
[Authorize]
public sealed class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpGet("api/folders/{folderId:guid}/documents")]
    [Authorize(Policy = AppPolicies.CanViewDocuments)]
    public async Task<IActionResult> List(Guid folderId, [FromQuery] DocumentQuery query, CancellationToken cancellationToken)
    {
        return Ok(await documentService.ListByFolderAsync(folderId, query, cancellationToken));
    }

    [HttpPost("api/folders/{folderId:guid}/documents")]
    [Authorize(Policy = AppPolicies.CanUploadDocuments)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(Guid folderId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            return BadRequest(new { message = "Debe enviar un archivo." });
        }

        await using var stream = file.OpenReadStream();
        var result = await documentService.UploadAsync(
            folderId,
            new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("api/documents/{id:guid}")]
    [Authorize(Policy = AppPolicies.CanViewDocuments)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await documentService.GetAsync(id, cancellationToken));
    }

    [HttpGet("api/documents/{id:guid}/content")]
    [Authorize(Policy = AppPolicies.CanViewDocuments)]
    public async Task<IActionResult> Content(Guid id, CancellationToken cancellationToken)
    {
        var content = await documentService.OpenContentAsync(id, download: false, cancellationToken);
        return File(content.Content, content.ContentType);
    }

    [HttpGet("api/documents/{id:guid}/download")]
    [Authorize(Policy = AppPolicies.CanDownloadDocuments)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var content = await documentService.OpenContentAsync(id, download: true, cancellationToken);
        return File(content.Content, content.ContentType, content.FileName);
    }

    [HttpDelete("api/documents/{id:guid}")]
    [Authorize(Policy = AppPolicies.CanDeleteDocuments)]
    public async Task<IActionResult> Delete(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken)
    {
        await documentService.DeleteAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("api/documents/{id:guid}/restore")]
    [Authorize(Policy = AppPolicies.CanRestoreDocuments)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        await documentService.RestoreAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("api/documents/{id:guid}/permanent")]
    [Authorize(Policy = AppPolicies.CanPermanentlyDeleteDocuments)]
    public async Task<IActionResult> PermanentlyDelete(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken)
    {
        await documentService.PermanentlyDeleteAsync(id, request, cancellationToken);
        return NoContent();
    }
}
