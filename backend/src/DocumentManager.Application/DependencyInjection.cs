using DocumentManager.Application.Interfaces;
using DocumentManager.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IFileValidationService, FileValidationService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ITrashService, TrashService>();
        return services;
    }
}
