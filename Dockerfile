# PeP Web Application Dockerfile
# Multi-stage build for optimal image size

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["PeP.csproj", "."]
RUN dotnet restore "PeP.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "PeP.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "PeP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# Install SQL Server tools (for health checks if needed)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Expose port (Cloud Run uses 8080 by default)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PeP.dll"]
