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
        // Request DTO
        public record Command(
            string LobbyName,
            string PLayerName,
            string ConnectionId,
            DiceSettings DiceSettings
        );



        // Validator
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.LobbyName)
                    .NotEmpty()
                    .WithMessage("Lobby name is required")
                    .MaximumLength(15)
                    .WithMessage("Lobby name cannot exceed 15 characters");

                RuleFor(x => x.PLayerName)
                    .NotEmpty()
                    .WithMessage("Player name is required")
                    .MaximumLength(15)
                    .WithMessage("Player name cannot exceed 15 characters")
                    .Matches("^[a-zA-Z]+$")
                    .WithMessage("Player name can only contain letters");

                RuleFor(x => x.ConnectionId)
                    .NotEmpty()
                    .WithMessage("Connection ID is required");

                RuleFor(x => x.DiceSettings)
                    .NotNull()
                    .WithMessage("Dice settings are required");

                RuleFor(x => x.DiceSettings.Count)
                    .InclusiveBetween(1, 10)
                    .WithMessage("Dice count must be between 1 and 10");

                RuleFor(x => x.DiceSettings.MinValue)
                    .GreaterThan(0)
                    .WithMessage("Min value must be greater than 0");

                RuleFor(x => x.DiceSettings.MaxValue)
                    .GreaterThan(x => x.DiceSettings.MinValue)
                    .WithMessage("Max value must be greater than min value");
            }
        }

        // Handler
        public static async Task Handle(Command command, MongoDbService database, IHubContext<GameHub> hub, CancellationToken cancellationToken = default)
        {
            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");

            var lobby = new DiceOnlineAPI.Models.Lobby
            {
                Id = Guid.NewGuid(),
                LobbyName = command.LobbyName,
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

            await hub.Clients.All.SendAsync("LobbyCreated", lobby, cancellationToken);
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
                    // Valideer het request
                    var validationResult = await validator.ValidateAsync(request);

                    if (!validationResult.IsValid)
                    {
                        return Results.BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    try
                    {
                        await Handle(request, database, hub);
                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                })
                .WithName("CreateLobby");
            }
        }
    }
}