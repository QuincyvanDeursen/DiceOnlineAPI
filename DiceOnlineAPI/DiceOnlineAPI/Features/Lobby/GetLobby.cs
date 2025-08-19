using Carter;
using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.DiceOnlineHub;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace DiceOnlineAPI.Features.Lobby
{
    public static class GetLobby
    {
        public record Request(string LobbyCode);

        public class RequestValidator : AbstractValidator<Request>
        {
            public RequestValidator()
            {
                RuleFor(x => x.LobbyCode)
                    .NotEmpty()
                    .WithMessage("Lobby code is required")
                    .Length(6)
                    .WithMessage("Lobby code must be exactly 6 characters")
                    .Matches("^[A-Z0-9]+$")
                    .WithMessage("Lobby code can only contain uppercase letters and numbers");
            }
        }

        public static async Task<DiceOnlineAPI.Models.Lobby> Handle(
            Request request,
            MongoDbService database,
            CancellationToken cancellationToken = default)
        {
            var collection = database.GetCollection<DiceOnlineAPI.Models.Lobby>("lobbies");
            var lobby = await collection.Find(l => l.LobbyCode == request.LobbyCode.ToUpper()).FirstOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Lobby not found");
            return lobby;
        }

        public class Endpoint : CarterModule
        {
            public override void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/lobbies/{lobbyCode}", async (
                     string lobbyCode,
                     MongoDbService database,
                     IHubContext<GameHub> hub,
                     IValidator<Request> validator) =>
                {
                    var request = new Request(LobbyCode: lobbyCode); 
                    var validationResult = await validator.ValidateAsync(request);
                    if (!validationResult.IsValid)
                    {
                        return Results.BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
                    }
                    try
                    {
                        var lobby = await Handle(request, database);
                        return Results.Ok(lobby);
                    }
                    catch (Exception ex)
                    {
                        return Results.NotFound(ex.Message);
                    }
                })
                .WithName("GetLobby")
                .Produces<DiceOnlineAPI.Models.Lobby>()
                .Produces(404);

            }
        }
    }
}
