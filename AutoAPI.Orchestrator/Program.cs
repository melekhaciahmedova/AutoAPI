using AutoAPI.Core.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services.AddSingleton<DockerService>();

// Container içinde 8080 portundan dinle
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Log environment info
Console.WriteLine($"🏗️ Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"🌍 Running in container: {Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")}");

var app = builder.Build();

// Swagger (Development ortamında aktif)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Nginx veya reverse proxy desteği
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();