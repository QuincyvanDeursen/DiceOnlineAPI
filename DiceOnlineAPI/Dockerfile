FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Kopieer alleen csproj
COPY ["DiceOnlineAPI/DiceOnlineAPI.csproj", "DiceOnlineAPI/"]
RUN dotnet restore "DiceOnlineAPI/DiceOnlineAPI.csproj"

# Kopieer de rest
COPY . .
WORKDIR "/src/DiceOnlineAPI"
RUN dotnet build "DiceOnlineAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DiceOnlineAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DiceOnlineAPI.dll"]
