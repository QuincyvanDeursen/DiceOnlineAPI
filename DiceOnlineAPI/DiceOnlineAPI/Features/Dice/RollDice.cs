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

        // ✅ Request DTO
        public record Command(
            string LobbyCode,
            string PlayerName,
            List<DiceOnlineAPI.Models.Dice> Dices
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

                RuleFor(x => x.Dices)
                    .NotNull()
                    .Must(dice => dice.Count > 0)
                    .WithMessage("At least one dice roll must be provided");

                RuleForEach(x => x.Dices).ChildRules(dice =>
                {
                    dice.RuleFor(d => d.Index).GreaterThanOrEqualTo(0);
                    dice.RuleFor(d => d.MinValue).GreaterThan(0);
                });
            }
        }

        // ✅ Handler
        public static async Task<object> Handle(
            Command command,
            MongoDbService database,
            IHubContext<GameHub> hub,
            CancellationToken cancellationToken = default)
        {
            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");

            var lobby = await collection.Find(l => l.LobbyCode == command.LobbyCode.ToUpper())
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Lobby not found");

            var random = new Random();
            var result = command.Dices.Select(d => new
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

            return result;
        }

        // ✅ Endpoint
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/lobbies/roll", async (
                    Command command,
                    MongoDbService database,
                    IHubContext<GameHub> hub,
                    IValidator<Command> validator) =>
                {
   
                    var validation = await validator.ValidateAsync(command);
                    if (!validation.IsValid)
                        return Results.BadRequest(validation.Errors.Select(e => e.ErrorMessage));

                    try
                    {
                        var result = await Handle(command, database, hub);
                        return Results.Ok(result);
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(ex.Message);
                    }
                })
                .WithName("RollDice"); ;
            }
        }
    }
}
