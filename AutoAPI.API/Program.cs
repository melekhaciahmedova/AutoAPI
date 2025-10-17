using AutoAPI.API.Services;
using AutoAPI.API.Services.Generation;
using AutoAPI.Core.Services;
using AutoAPI.Data.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoAPI",
        Version = "v1",
        Description = "Automatic Entity Generator API"
    });
});

builder.Services.AddHttpClient();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("MSSQL"));
});

builder.Services.AddScoped<ITemplateRenderer, TemplateRenderer>();
builder.Services.AddScoped<SwaggerParserService>();
builder.Services.AddScoped<DockerService>();
builder.Services.AddSingleton<MigrationWatcherService>();

builder.Services.AddHttpClient();

builder.Services.AddScoped(provider =>
    new MigrationGeneratorService(provider.GetRequiredService<IWebHostEnvironment>().ContentRootPath)
);

builder.WebHost.UseUrls("http://0.0.0.0:8080"); // Container içi 8080'de dinle

// Log environment
Console.WriteLine($"🏗️  Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"🌍 Running in container: {Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")}");

var app = builder.Build();

app.UseSwagger(c =>
{
    c.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "AutoAPI v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.MapControllers();
app.Run();