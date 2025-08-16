using Microsoft.AspNetCore.SignalR;

namespace DiceOnlineAPI.DiceOnlineHub
{
    public class GameHub : Hub
    {
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
    }
}
