using Carter;

namespace DiceOnlineAPI.Features.Health
{
    public static class CheckHealth
    {

        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/health", () => Results.Ok("I'm Alive!"))
                    .WithName("CheckHealth");
            }
        }
    }
}
