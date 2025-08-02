using Carter;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace DiceOnlineAPI.Features.Dice
{
    public static class RollDice
    {
        // ✅ Dice info per dobbelsteen
        public record DiceRollInput(int Index, int MinValue, int MaxValue);

        // ✅ Request DTO
        public record Command(
            string LobbyCode,
            string PlayerName,
            string ConnectionId,
            List<DiceRollInput> Dice
        );

        // ✅ Validator
        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.LobbyCode)
                    .NotEmpty();

                RuleFor(x => x.PlayerName)
                    .NotEmpty()
                    .MaximumLength(15)
                    .Matches("^[a-zA-Z]+$");

                RuleFor(x => x.ConnectionId)
                    .NotEmpty();

                RuleFor(x => x.Dice)
                    .NotNull()
                    .Must(dice => dice.Count > 0)
                    .WithMessage("At least one dice roll must be provided");

                RuleForEach(x => x.Dice).ChildRules(dice =>
                {
                    dice.RuleFor(d => d.Index).GreaterThanOrEqualTo(0);
                    dice.RuleFor(d => d.MinValue).GreaterThan(0);
                    dice.RuleFor(d => d.MaxValue)
                        .GreaterThan(d => d.MinValue);
                });
            }
        }

        // ✅ Handler
        public static async Task Handle(
            Command command,
            MongoDbService database,
            IHubContext<GameHub> hub,
            CancellationToken cancellationToken = default)
        {
            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");

            var lobby = await collection.Find(l => l.LobbyCode == command.LobbyCode)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Lobby not found");

            if (!lobby.Players.Contains(command.PlayerName))
                throw new Exception("Player not in lobby");

            var random = new Random();
            var result = command.Dice.Select(d => new
            {
                d.Index,
                Value = random.Next(d.MinValue, d.MaxValue + 1)
            }).ToList();

            await hub.Clients.Group(lobby.LobbyCode)
                .SendAsync("DiceRolled", new
                {
                    command.PlayerName,
                    Results = result
                }, cancellationToken);
        }

        // ✅ Endpoint
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/lobbies/{lobbyCode}/roll", async (
                    string lobbyCode,
                    Command command,
                    MongoDbService database,
                    IHubContext<GameHub> hub,
                    IValidator<Command> validator) =>
                {
                    // Overschrijd lobbyCode in body als je consistent wilt zijn:
                    command = command with { LobbyCode = lobbyCode };

                    var validation = await validator.ValidateAsync(command);
                    if (!validation.IsValid)
                        return Results.BadRequest(validation.Errors.Select(e => e.ErrorMessage));

                    try
                    {
                        await Handle(command, database, hub);
                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                })
                .WithName("RollDice");
            }
        }
    }
}
