using Carter;
using DiceOnlineAPI.Database.Service;
using FluentValidation;
using System.Reflection;

namespace DiceOnlineAPI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDiceOnlineServices(this IServiceCollection services)
        {
            // Carter voor endpoints
            services.AddOpenApi();
            services.AddCarter();

            // MongoDB service
            services.AddSingleton<MongoDbService>();

            // FluentValidation - scan hele assembly
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); // Ensure FluentValidation is installed and referenced

            // SignalR
            services.AddSignalR();

            return services;
        }
    }

}