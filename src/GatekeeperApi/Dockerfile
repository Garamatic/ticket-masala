# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-chiseled AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/GatekeeperApi/GatekeeperApi.csproj", "GatekeeperApi/"]
COPY ["src/TicketMasala.Web/TicketMasala.Web.csproj", "TicketMasala.Web/"]
COPY ["src/TicketMasala.Domain/TicketMasala.Domain.csproj", "TicketMasala.Domain/"]
RUN dotnet restore "GatekeeperApi/GatekeeperApi.csproj"

# Copy everything and build
COPY src/ .
WORKDIR "/src/GatekeeperApi"
RUN dotnet publish "GatekeeperApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GatekeeperApi.dll"]
