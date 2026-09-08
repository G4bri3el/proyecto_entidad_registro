using System.Text;
using DocumentManager.Application.Configuration;
using DocumentManager.Application.Services;
using DocumentManager.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace DocumentManager.UnitTests;

public sealed class FileValidationServiceTests
{
    private readonly FileValidationService _service = new(Options.Create(new FileUploadOptions()));

    [Fact]
    public async Task Rejects_executable_renamed_as_pdf()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("MZ fake exe"));

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _service.ValidateAsync(stream, "archivo.pdf", "application/pdf", stream.Length));
    }

    [Fact]
    public async Task Accepts_valid_pdf_signature()
    {
        await using var stream = new MemoryStream("%PDF-1.7 test"u8.ToArray());

        var result = await _service.ValidateAsync(stream, "contrato.pdf", "application/pdf", stream.Length);

        Assert.Equal(".pdf", result.Extension);
        Assert.Equal("application/pdf", result.MimeType);
    }

    [Fact]
    public async Task Rejects_false_content_type()
    {
        await using var stream = new MemoryStream("%PDF-1.7 test"u8.ToArray());

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _service.ValidateAsync(stream, "contrato.pdf", "image/png", stream.Length));
    }

    [Fact]
    public async Task Rejects_files_above_limit()
    {
        var service = new FileValidationService(Options.Create(new FileUploadOptions { MaxFileSizeMb = 1 }));
        await using var stream = new MemoryStream("%PDF-1.7 test"u8.ToArray());

        var exception = await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.ValidateAsync(stream, "contrato.pdf", "application/pdf", 2 * 1024 * 1024));

        Assert.Equal(413, exception.StatusCode);
    }
}
