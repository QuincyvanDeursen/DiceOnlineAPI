using Carter;
using DiceOnlineAPI.Database.Indexes;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using DiceOnlineAPI.Extensions;
using Scalar.AspNetCore;

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

// TTL-indexen aanmaken bij startup
var mongoService = app.Services.GetRequiredService<MongoDbService>();
MongoDbIndex.EnsureIndexes(mongoService);

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
app.UseCors("AngularApp");

// Endpoints
app.MapHub<GameHub>("/gamehub");
app.MapCarter();

app.Run();