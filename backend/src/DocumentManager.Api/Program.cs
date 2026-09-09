using System.Threading.RateLimiting;
using DocumentManager.Api.Middleware;
using DocumentManager.Application;
using DocumentManager.Application.Configuration;
using DocumentManager.Infrastructure;
using DocumentManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    DotEnvLoader.LoadNearest(Directory.GetCurrentDirectory(), AppContext.BaseDirectory);

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console();
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Document Manager API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });
    });

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("frontend", policy =>
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var rateOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = rateOptions.AuthPermitLimit;
            limiter.Window = TimeSpan.FromSeconds(rateOptions.AuthWindowSeconds);
            limiter.QueueLimit = 0;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
        options.AddFixedWindowLimiter("refresh", limiter =>
        {
            limiter.PermitLimit = rateOptions.RefreshPermitLimit;
            limiter.Window = TimeSpan.FromSeconds(rateOptions.RefreshWindowSeconds);
            limiter.QueueLimit = 0;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    });

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseMiddleware<ActiveUserMiddleware>();
    app.UseMiddleware<CsrfValidationMiddleware>();
    app.UseAuthorization();
    app.MapControllers();

    await DevelopmentDataSeeder.SeedAsync(app.Services);
    await app.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
