using Carter;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace DiceOnlineAPI.Features.Chat
{
    public static class SendMessage
    {
        public record Command(
            string LobbyCode,
            string PlayerName,
            string Message
         );

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
                RuleFor(x => x.Message)
                    .NotEmpty()
                    .WithMessage("Message cannot be empty")
                    .MaximumLength(200)
                    .WithMessage("Message cannot exceed 200 characters");
            }
        }

        public static async Task Handle(
            Command command,
            MongoDbService database,
            IHubContext<GameHub> hub,
            CancellationToken cancellationToken = default)
        {
            await hub.Clients.Group(command.LobbyCode).SendAsync("MessageSent", command.PlayerName, command.Message, cancellationToken);
        }

        public class Endpoint : CarterModule
        {
            public override void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/lobbies/message", async (
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
                        return Results.NotFound(ex.Message);
                    }
                }); ;
            }
        }
    }
}
