using Carter;
using DiceOnlineAPI.DiceOnlineHub;
using DiceOnlineAPI.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddDiceOnlineServices();

// HARDCODED CORS - geen configuratie gedoe
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy
            .WithOrigins("https://playdice.app") // Alleen production
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// CORS heel vroeg
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