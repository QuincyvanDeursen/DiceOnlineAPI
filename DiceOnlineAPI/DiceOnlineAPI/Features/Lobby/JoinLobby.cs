using Carter;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using DiceOnlineAPI.Models;
using DiceOnlineAPI.Shared.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Linq;

namespace DiceOnlineAPI.Features.Lobby
{
    public static class JoinLobby
    {
        // Command met ConnectionId
        public record Command(string LobbyCode, string PlayerName, string ConnectionId);

        //validator
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.LobbyCode)
                    .NotEmpty()
                    .WithMessage("Lobby code is required")
                    .Length(6)
                    .WithMessage("Lobby code must be exactly 6 characters")
                    .Matches("^[A-Za-z0-9]+$")
                    .WithMessage("Lobby code can only contain letters and numbers");

                RuleFor(x => x.PlayerName)
                    .NotEmpty()
                    .WithMessage("Player name is required")
                    .MaximumLength(15)
                    .WithMessage("Player name cannot exceed 15 characters")
                    .Matches("^[a-zA-Z]+$")
                    .WithMessage("Player name can only contain letters");

                RuleFor(x => x.ConnectionId)
                    .NotEmpty()
                    .WithMessage("Connection ID is required");
            }
        }

        // Handler

        public static async Task Handle(Command command, MongoDbService database, IHubContext<GameHub> hub, CancellationToken cancellationToken = default)
        {

            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");
            var lobby = await collection.Find(l => l.LobbyCode == command.LobbyCode).FirstOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Lobby not found");

            var nameWithoutCheat = command.PlayerName.Replace("cheat", string.Empty, StringComparison.OrdinalIgnoreCase);
            // Voeg speler toe aan lobby
            if (!CheckIfPlayerNameExists(lobby.Players, nameWithoutCheat))
            {
                // Replace the problematic line with the following code:
                if (!lobby.Players.Any(p => p.Name == nameWithoutCheat))
                    lobby.Players.Add(new Player { Name = nameWithoutCheat, ConnectionId = command.ConnectionId });
                var update = Builders<DiceOnlineAPI.Models.Lobby>.Update
                    .Set(l => l.Players, lobby.Players)
                    .Set(l => l.UpdatedAt, TimeHelper.AmsterdamNow);
                await collection.UpdateOneAsync(l => l.Id == lobby.Id, update, cancellationToken: cancellationToken);
            }
            else
            {
                throw new Exception($"There already is a player named {nameWithoutCheat} in this lobby");
            }
            await hub.Groups.AddToGroupAsync(command.ConnectionId, lobby.LobbyCode, cancellationToken);
            await hub.Clients.GroupExcept(lobby.LobbyCode, command.ConnectionId)
              .SendAsync("PlayerJoined", nameWithoutCheat, cancellationToken);
        }

        private static bool CheckIfPlayerNameExists(List<Player> players, string playerName)
        {
            var exists = false;
            players.ForEach(player =>
            {
                if (player.Name.ToLower() ==playerName.ToLower())
                {
                    exists = true;
                }
            });
            return exists;
        }


        // Endpoint
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/lobbies/join", async (
                     Command request,
                     MongoDbService database,
                     IHubContext<GameHub> hub,
                     IValidator<Command> validator) =>
                {
                    // Valideer het command
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
                .WithName("JoinLobby");
            }
        }
    }
}
