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

# Create standard mount points and tenant directories
RUN mkdir -p /app/inputs/config /app/inputs/data /app/keys \
    /app/tenants/default/config /app/tenants/default/data /app/tenants/default/theme \
    /app/tenants/government/config /app/tenants/government/data /app/tenants/government/theme \
    /app/tenants/healthcare/config /app/tenants/healthcare/data /app/tenants/healthcare/theme \
    /app/tenants/helpdesk/config /app/tenants/helpdesk/data /app/tenants/helpdesk/theme \
    /app/tenants/landscaping/config /app/tenants/landscaping/data /app/tenants/landscaping/theme \
    /app/wwwroot/tenant-theme

# Create a non-root user for security
RUN groupadd --system --gid 1001 masala && \
    useradd --system --uid 1001 --gid masala --shell /bin/sh masala

# Copy the binary from build stage (do this as root, then chown)
COPY --from=build /app/publish .

# Copy all tenant configurations and themes
COPY --chown=masala:masala tenants/default/ /app/tenants/default/
COPY --chown=masala:masala tenants/government/ /app/tenants/government/
COPY --chown=masala:masala tenants/healthcare/ /app/tenants/healthcare/
COPY --chown=masala:masala tenants/helpdesk/ /app/tenants/helpdesk/
COPY --chown=masala:masala tenants/landscaping/ /app/tenants/landscaping/

# Copy default configuration for backward compatibility
COPY --chown=masala:masala tenants/default/config/ /app/inputs/config/

# Client files are served dynamically from tenant directories

# Set proper permissions
RUN chown -R masala:masala /app/inputs /app/keys /app/tenants /app/wwwroot

# Ensure the app files are owned by the non-root user, then switch user
RUN chown -R masala:masala /app

USER masala

# ENVIRONMENT DEFAULTS
ENV MASALA_CONFIG_PATH="/app/inputs/config"
ENV MASALA_DB_PATH="/app/inputs/data/masala.db"
ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080

ENTRYPOINT ["dotnet", "TicketMasala.Web.dll"]
