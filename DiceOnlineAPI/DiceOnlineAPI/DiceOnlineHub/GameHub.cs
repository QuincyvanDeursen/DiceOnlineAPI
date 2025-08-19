using DiceOnlineAPI.Database.Service;
using DiceOnlineAPI.Models;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace DiceOnlineAPI.DiceOnlineHub
{
    public class GameHub : Hub
    {

        private readonly MongoDbService _database;

        public GameHub(MongoDbService database)
        {
            _database = database;
        }

        // Simple get method to retrieve the connection ID of the player (no endpoint)
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        // Method to leave a lobby when the player disconnects (by closing the browser or navigating away)
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var collection = _database.GetCollection<Lobby>("lobbies");
            var lobby = await collection
                .Find(l => l.Players.Any(p => p.ConnectionId == Context.ConnectionId))
                .FirstOrDefaultAsync();

            if (lobby != null)
            {
                var player = lobby.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    await RemovePlayerFromLobby(lobby, player.Name, Context.ConnectionId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task RemovePlayerFromLobby(Lobby lobby, string playerName, string connectionId)
        {
            var player = lobby.Players.FirstOrDefault(p => p.Name == playerName && p.ConnectionId == connectionId);
            if (player != null)
            {
                lobby.Players.Remove(player);
                var update = Builders<Lobby>.Update
                    .Set(l => l.Players, lobby.Players)
                    .Set(l => l.UpdatedAt, DateTime.UtcNow);

                var collection = _database.GetCollection<Lobby>("lobbies");
                await collection.UpdateOneAsync(l => l.Id == lobby.Id, update);

                // Notify andere spelers
                await Clients.Group(lobby.LobbyCode).SendAsync("PlayerLeft", playerName);
            }
        }
    }
}
