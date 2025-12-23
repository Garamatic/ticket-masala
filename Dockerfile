# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers (Caching Strategy)
COPY ["src/TicketMasala.Domain/TicketMasala.Domain.csproj", "src/TicketMasala.Domain/"]
COPY ["src/TicketMasala.Web/TicketMasala.Web.csproj", "src/TicketMasala.Web/"]
RUN dotnet restore "src/TicketMasala.Web/TicketMasala.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/TicketMasala.Web"
RUN dotnet publish "TicketMasala.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Prepare Layout (Since Chiseled has no shell/mkdir)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS prepare
WORKDIR /app
COPY --from=build /app/publish .

# Create directory structure
RUN mkdir -p /app/inputs/config /app/inputs/data /app/keys \
    /app/tenants/_template/config /app/tenants/_template/data /app/tenants/_template/theme \
    /app/wwwroot/tenant-theme

# Copy templates
COPY tenants/_template/ /app/tenants/_template/

# Set permissions for the 'app' user (UID 1654 in Chiseled)
RUN chown -R 1654:1654 /app

# STAGE 3: Runtime (Standard Ubuntu Noble - Includes ICU & Shell)
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble AS final
WORKDIR /app

# Copy everything from the prepare stage with correct permissions
COPY --from=prepare --chown=1654:1654 /app .

# ENVIRONMENT DEFAULTS
ENV MASALA_CONFIG_PATH="/app/inputs/config"
ENV MASALA_DB_PATH="/app/inputs/data/masala.db"
ENV ASPNETCORE_URLS="http://+:8080"

# Expose volumes (Note: Chiseled cannot create these at runtime, they must bind mount to existing dirs)
# In Fly.io, we mount volumes here.

EXPOSE 8080

# Use the built-in app user
# USER 1654

ENTRYPOINT ["dotnet", "TicketMasala.Web.dll"]
