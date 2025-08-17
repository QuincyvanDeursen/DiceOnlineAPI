using Carter;
using DiceOnlineAPI.DiceOnlineHub;
using DiceOnlineAPI.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddDiceOnlineServices();

// CORS TERUG VOOR SIGNALR
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("https://playdice.app")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// CORS middleware voor SignalR
app.UseCors();

// EXTRA CORS HEADERS voor regular endpoints
app.Use(async (context, next) =>
{
    // Alleen toevoegen als ze er nog niet zijn (om SignalR niet te breken)
    if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "https://playdice.app");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }

    // Handle OPTIONS preflight requests
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.WriteAsync("");
        return;
    }

    await next();
});

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

// Debug middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request from: {context.Request.Headers["Origin"]}");
    Console.WriteLine($"Request method: {context.Request.Method}");
    Console.WriteLine($"Request path: {context.Request.Path}");
    await next();
});

// Endpoints
app.MapHub<GameHub>("/gamehub");
app.MapCarter();

app.Run();