# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers (Caching Strategy)
COPY ["src/TicketMasala.Domain/TicketMasala.Domain.csproj", "src/TicketMasala.Domain/"]
COPY ["src/RabbitMqConnector/RabbitMqConnector/RabbitMqConnector.csproj", "src/RabbitMqConnector/RabbitMqConnector/"]
COPY ["src/TicketMasala.Web/TicketMasala.Web.csproj", "src/TicketMasala.Web/"]
RUN dotnet restore "src/TicketMasala.Web/TicketMasala.Web.csproj" -r linux-x64

# Copy everything else and publish
COPY . .
WORKDIR "/src/src/TicketMasala.Web"
RUN dotnet publish "TicketMasala.Web.csproj" -c Release -r linux-x64 -o /app/publish /p:UseAppHost=false /p:DebugSymbols=false /p:DebugType=none --no-restore

# STAGE 2: Prepare Layout (Chiseled has no shell/mkdir, so we do it here)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS prepare
WORKDIR /app
COPY --from=build /app/publish .

# Strip native debug symbols and documentation files to reduce image size
RUN find /app -name "*.dbg" -delete && \
    find /app -maxdepth 1 -name "*.xml" -delete

# Create directory structure and copy templates
# NOTE: /app/config and /app/data are required as mountpoints for the demo compose
RUN mkdir -p /app/config /app/data /app/inputs/config /app/inputs/data /app/keys \
    /app/tenants/_template/config /app/tenants/_template/data /app/tenants/_template/theme \
    /app/wwwroot/tenant-theme \
    && touch /app/config/seed_data.json
COPY tenants/_template/ /app/tenants/_template/

# Copy all tenant configurations from local config directory
COPY config/tenants/desgoffe /app/tenants/desgoffe
COPY config/tenants/whitman /app/tenants/whitman
COPY config/tenants/liberty /app/tenants/liberty
COPY config/tenants/hennessey /app/tenants/hennessey

# Sync tenant assets from canonical config/tenants/ to wwwroot/tenants/
# (single source of truth: config/tenants/ owns the theme and logo)
RUN for tenant in desgoffe whitman liberty hennessey; do \
    mkdir -p /app/wwwroot/tenants/$tenant && \
    cp /app/tenants/$tenant/theme/style.css /app/wwwroot/tenants/$tenant/style.css && \
    cp /app/tenants/$tenant/$tenant.png /app/wwwroot/tenants/$tenant/logo.png; \
    done

# STAGE 3: Runtime (Chiseled Noble Extra - Includes ICU, Minimal surface)
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app

# Copy everything from the prepare stage with correct permissions
COPY --from=prepare --chown=1654:1654 /app .

# ENVIRONMENT DEFAULTS
# MASALA_TENANT can be overridden at runtime to switch tenants
ENV MASALA_TENANT="desgoffe" \
    MASALA_CONFIG_PATH="/app/tenants/desgoffe/config" \
    MASALA_DB_PATH="/app/inputs/data/masala.db" \
    ASPNETCORE_URLS="http://+:8080" \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

# Use the built-in app user
USER 1654

ENTRYPOINT ["dotnet", "TicketMasala.Web.dll"]
