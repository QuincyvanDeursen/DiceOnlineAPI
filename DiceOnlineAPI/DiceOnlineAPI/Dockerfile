FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Kopieer alleen csproj (het pad is relatief t.o.v. Dockerfile)
COPY ["DiceOnlineAPI.csproj", "."]
RUN dotnet restore "DiceOnlineAPI.csproj"

# Kopieer de rest van de code INCLUSIEF appsettings bestanden
COPY . .


RUN dotnet build "DiceOnlineAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiceOnlineAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Kopieer gepubliceerde app
COPY --from=publish /app/publish .

# Zorg ervoor dat appsettings bestanden in de final image zitten
COPY --from=publish /app/publish/appsettings*.json ./

# Set environment variabelen voor CORS (fallback)
ENV Cors__AllowedOrigins__0=https://playdice.app

ENTRYPOINT ["dotnet", "DiceOnlineAPI.dll"]