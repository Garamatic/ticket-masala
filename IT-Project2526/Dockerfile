# Multi-stage build for IT-Project2526 (.NET 8)
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# We assume build context is the project directory (IT-Project2526)
# Copy project file and restore first for better caching
COPY IT-Project2526.csproj ./
RUN dotnet restore IT-Project2526.csproj

# Copy the rest of the source
COPY . .

# Publish (no restore needed because already restored)
RUN dotnet publish IT-Project2526.csproj -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Ensure SQLite data directory exists (will be mount point on Fly)
RUN mkdir -p /data

# Copy published output
COPY --from=build /app/publish .

# Configure Kestrel to listen on 0.0.0.0:8080 (Fly expects internal_port 8080)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "IT-Project2526.dll"]
