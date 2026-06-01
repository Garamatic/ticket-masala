# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers (Caching Strategy)
COPY ["src/GatekeeperApi/GatekeeperApi.csproj", "GatekeeperApi/"]
COPY ["src/TicketMasala.Domain/TicketMasala.Domain.csproj", "TicketMasala.Domain/"]
COPY ["src/RabbitMqConnector/RabbitMqConnector/RabbitMqConnector.csproj", "RabbitMqConnector/RabbitMqConnector/"]
RUN dotnet restore "GatekeeperApi/GatekeeperApi.csproj"

# Copy everything and publish
COPY src/ .
WORKDIR "/src/GatekeeperApi"
RUN dotnet publish "GatekeeperApi.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GatekeeperApi.dll"]
