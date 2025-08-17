using Carter;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using MongoDB.Driver.Core.Connections;

namespace DiceOnlineAPI.Features.Lobby
{
    public static class LeaveLobby
    {
        public record Command(string LobbyCode, string PlayerName, string ConnectionId);
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.LobbyCode)
                    .NotEmpty()
                    .WithMessage("Lobby code is required")
                    .Length(6)
                    .WithMessage("Lobby code must be exactly 6 characters")
                    .Matches("^[A-Z0-9]+$")
                    .WithMessage("Lobby code can only contain uppercase letters and numbers");
                RuleFor(x => x.PlayerName)
                    .NotEmpty()
                    .WithMessage("Player name is required")
                    .MaximumLength(15)
                    .WithMessage("Player name cannot exceed 15 characters")
                    .Matches("^[a-zA-Z]+$")
                    .WithMessage("Player name can only contain letters");
            }
        }
        public static async Task Handle(
            Command command,
            MongoDbService database,
            IHubContext<GameHub> hub,
            CancellationToken cancellationToken = default)
        {
            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");
            var lobby = await collection.Find(l => l.LobbyCode == command.LobbyCode).FirstOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Lobby not found");
            // Verwijder speler uit lobby
            var playerName = command.PlayerName;
            if (lobby.Players.Contains(playerName))
            {
                lobby.Players.Remove(playerName);
                var update = Builders<DiceOnlineAPI.Models.Lobby>.Update
                    .Set(l => l.Players, lobby.Players)
                    .Set(l => l.UpdatedAt, DateTime.UtcNow);
                await collection.UpdateOneAsync(l => l.Id == lobby.Id, update, cancellationToken: cancellationToken);
            }
            else
            {
                throw new Exception("Player not found in lobby");
            }
            await hub.Clients.Group(command.LobbyCode).SendAsync("PlayerLeft", command.PlayerName, cancellationToken);
            await hub.Groups.RemoveFromGroupAsync(command.ConnectionId, command.LobbyCode);
        }
        public class Endpoint : CarterModule
        {
            public override void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/lobbies/leave", async (
                    Command command,
                    MongoDbService database,
                    IHubContext<GameHub> hub,
                    IValidator<Command> validator) =>
                {
                    var validationResult = await validator.ValidateAsync(command);
                    if (!validationResult.IsValid)
                    {
                        return Results.BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
                    }

                    try
                    {
                        await Handle(command, database, hub);
                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                });
            }
        }
    }
}
