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

# Build a minimal healthcheck probe (no shell/curl in chiseled images)
WORKDIR /tmp/healthcheck
RUN dotnet new console --force && \
    cat <<'EOF' > Program.cs
using System.Net.Http;
class Program {
    static int Main() {
        try {
            var client = new HttpClient { Timeout = System.TimeSpan.FromSeconds(5) };
            var resp = client.GetAsync("http://localhost:8080/health").Result;
            return resp.IsSuccessStatusCode ? 0 : 1;
        } catch { return 1; }
    }
}
EOF
RUN dotnet publish -c Release -o /app/healthcheck --no-restore

# Runtime Stage (Chiseled — minimal attack surface)
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /app/healthcheck /app/healthcheck

# Use the built-in non-root app user (UID 1654)
USER 1654

HEALTHCHECK --interval=10s --timeout=5s --start-period=60s --retries=6 \
    CMD ["dotnet", "/app/healthcheck/healthcheck.dll"]

ENTRYPOINT ["dotnet", "GatekeeperApi.dll"]
