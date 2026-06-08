# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers (Caching Strategy)
COPY ["src/GatekeeperApi/GatekeeperApi.csproj", "GatekeeperApi/"]
COPY ["src/TicketMasala.Domain/TicketMasala.Domain.csproj", "TicketMasala.Domain/"]
COPY ["src/RabbitMqConnector/RabbitMqConnector/RabbitMqConnector.csproj", "RabbitMqConnector/RabbitMqConnector/"]
RUN dotnet restore "GatekeeperApi/GatekeeperApi.csproj" -r linux-x64

# Copy everything and publish
COPY src/ .
WORKDIR "/src/GatekeeperApi"
RUN dotnet publish "GatekeeperApi.csproj" -c Release -r linux-x64 -o /app/publish /p:UseAppHost=false /p:DebugSymbols=false /p:DebugType=none --no-restore

# Runtime Stage (Chiseled — minimal attack surface)
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app

COPY --from=build /app/publish .

# Use the built-in non-root app user (UID 1654)
USER 1654

ENTRYPOINT ["dotnet", "GatekeeperApi.dll"]
