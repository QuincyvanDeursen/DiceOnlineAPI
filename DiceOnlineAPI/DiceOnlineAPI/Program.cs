using Carter;
using DiceOnlineAPI.DiceOnlineHub;
using DiceOnlineAPI.Extensions;
using Scalar.AspNetCore;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddDiceOnlineServices();

// Cors
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Nodig voor SignalR
    });
});

var app = builder.Build();
app.UseCors("AngularApp");
// Development tools
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("DiceOnline API")
        .WithTheme(ScalarTheme.Saturn)
        .WithDarkMode());
}

app.UseHttpsRedirection();


// Endpoints
app.MapHub<GameHub>("/gamehub");
app.MapCarter();

Console.WriteLine("=== CHECKING CONFIGURATION FILES ===");
Console.WriteLine($"appsettings.json exists: {File.Exists("appsettings.json")}");
Console.WriteLine($"appsettings.Production.json exists: {File.Exists("appsettings.Production.json")}");
Console.WriteLine("=== CONFIGURATION DEBUG ===");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

// Test of configuratie wordt ingelezen
var corsSection = builder.Configuration.GetSection("Cors");
Console.WriteLine($"Cors section exists: {corsSection.Exists()}");

var allowedOriginsSection = builder.Configuration.GetSection("Cors:AllowedOrigins");
Console.WriteLine($"AllowedOrigins section exists: {allowedOriginsSection.Exists()}");

// Test wat er daadwerkelijk wordt ingelezen
var allowedOrigins2 = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
Console.WriteLine($"Loaded {allowedOrigins2.Length} origins:");
foreach (var origin in allowedOrigins2)
{
    Console.WriteLine($"  - '{origin}'");
}

// Test ook alle configuratie keys
Console.WriteLine("All configuration keys:");
foreach (var item in builder.Configuration.AsEnumerable())
{
    if (item.Key.Contains("Cors"))
    {
        Console.WriteLine($"  {item.Key} = {item.Value}");
    }
}
Console.WriteLine("=== END CONFIGURATION DEBUG ===");

app.Run();