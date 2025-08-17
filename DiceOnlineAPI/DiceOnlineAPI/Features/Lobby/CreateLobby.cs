using Carter;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using DiceOnlineAPI.Models;
using DiceOnlineAPI.Shared.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;

namespace DiceOnlineAPI.Features.Lobby
{
    public static class CreateLobby
    {
        // Fix for CS0060: Ensure consistent accessibility by making the Command record public.
        public record Command(
            string PLayerName,
            string ConnectionId,
            DiceSettings DiceSettings
        );



        // Validator
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.PLayerName)
                    .NotEmpty()
                    .WithMessage("Player name is required")
                    .MaximumLength(20)
                    .WithMessage("Player name cannot exceed 20 characters")
                    .Matches("^[a-zA-Z]+$")
                    .WithMessage("Player name can only contain letters");

                RuleFor(x => x.ConnectionId)
                    .NotEmpty()
                    .WithMessage("Connection ID is required");

                RuleFor(x => x.DiceSettings)
                    .NotNull()
                    .WithMessage("Dice settings are required");

                RuleFor(x => x.DiceSettings.Count)
                    .InclusiveBetween(1, 8)
                    .WithMessage("Dice count must be between 1 and 8");

                RuleFor(x => x.DiceSettings.MinValue)
                    .GreaterThan(0)
                    .WithMessage("Min value must be greater than 0");

                RuleFor(x => x.DiceSettings.MaxValue)
                    .GreaterThan(x => x.DiceSettings.MinValue)
                    .WithMessage("Max value must be greater than min value");
            }
        }

        // Handler
        private static async Task<string> Handle(
            Command command,
            MongoDbService database,
            IHubContext<GameHub> hub,
            CancellationToken cancellationToken = default)
        {
            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");

            var lobby = new DiceOnlineAPI.Models.Lobby
            {
                Id = Guid.NewGuid(),
                LobbyCode = GenerateLobbyCode(),
                Players = new List<string> { command.PLayerName },
                DiceSettings = new DiceOnlineAPI.Models.DiceSettings
                {
                    Count = command.DiceSettings.Count,
                    MinValue = command.DiceSettings.MinValue,
                    MaxValue = command.DiceSettings.MaxValue
                },
                CreatedAt = TimeHelper.AmsterdamNow,
                UpdatedAt = TimeHelper.AmsterdamNow
            };

            await collection.InsertOneAsync(lobby, cancellationToken: cancellationToken);
            await hub.Groups.AddToGroupAsync(command.ConnectionId, lobby.LobbyCode, cancellationToken);

            return lobby.LobbyCode; // <-- teruggeven
        }

        private static string GenerateLobbyCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Endpoint
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/lobbies", async (
                    Command request,
                    MongoDbService database,
                    IHubContext<GameHub> hub,
                    IValidator<Command> validator) =>
                {
                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        return Results.BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    try
                    {
                        var lobbyCode = await Handle(request, database, hub);
                        return Results.Ok(lobbyCode);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                })
                .WithName("CreateLobby")
               ;
            }
        }
    }
}