# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers (Caching Strategy)
COPY ["src/TicketMasala.Web/TicketMasala.Web.csproj", "src/TicketMasala.Web/"]
RUN dotnet restore "src/TicketMasala.Web/TicketMasala.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/TicketMasala.Web"
RUN dotnet publish "TicketMasala.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Runtime (The "Lite" Image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create standard mount points
# We create them here so permissions can be set correctly
RUN mkdir -p /app/config /app/data

# Create a non-root user for security
RUN groupadd --system --gid 1001 masala && \
    useradd --system --uid 1001 --gid masala --shell /bin/sh masala

# Chown the directories so the app can write to Data but only Read config
# (Config is technically read-only, but the user needs access to list files)
RUN chown -R masala:masala /app/data && \
    chown -R masala:masala /app/config

USER masala

# Copy the binary from build stage
COPY --from=build --chown=masala:masala /app/publish .

# ENVIRONMENT DEFAULTS
ENV MASALA_CONFIG_PATH="/app/config"
ENV MASALA_DB_PATH="/app/data/masala.db"
ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080

ENTRYPOINT ["dotnet", "TicketMasala.Web.dll"]
