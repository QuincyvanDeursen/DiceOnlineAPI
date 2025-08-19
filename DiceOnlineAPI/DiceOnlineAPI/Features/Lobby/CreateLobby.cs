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
            string PlayerName,
            string ConnectionId,
            List<DiceOnlineAPI.Models.Dice> Dices
        );



        // Validator
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.PlayerName)
                    .NotEmpty()
                    .WithMessage("Player name is required")
                    .MaximumLength(20)
                    .WithMessage("Player name cannot exceed 20 characters")
                    .Matches("^[a-zA-Z]+$")
                    .WithMessage("Player name can only contain letters");

                RuleFor(x => x.ConnectionId)
                    .NotEmpty()
                    .WithMessage("Connection ID is required");

                RuleFor(x => x.Dices)
                    .NotNull()  
                    .WithMessage("Dices are required");

                RuleFor(x => x.Dices.Count)
                    .InclusiveBetween(1, 8)
                    .WithMessage("Dice count must be between 1 and 8");

                RuleForEach(x => x.Dices)
                    .NotEmpty()
                    .WithMessage("Each dice must have a value")
                    .Must(d => d.MinValue >= 1 && d.MaxValue <= 20)
                    .WithMessage("Dice values must be between 1 and 20")
                    .Must(d => d.MinValue <= d.MaxValue)
                    .WithMessage("Min value must be less or equal than Max value");


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
            var nameWithoutCheat = command.PlayerName.Replace("cheat", string.Empty, StringComparison.OrdinalIgnoreCase);
            var lobby = new DiceOnlineAPI.Models.Lobby
            {
                Id = Guid.NewGuid(),
                LobbyCode = GenerateLobbyCode(),
                Players = new List<Player>
                {
                    new Player { Name = nameWithoutCheat, ConnectionId = command.ConnectionId }
                },
                Dices = command.Dices,
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