# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file
COPY Infrastructure.sln ./

# Copy all project files for dependency restoration
COPY src/Infrastructure.MultiTenancy/Infrastructure.MultiTenancy.csproj src/Infrastructure.MultiTenancy/
COPY src/Infrastructure.Configuration/Infrastructure.Configuration.csproj src/Infrastructure.Configuration/
COPY src/Infrastructure.Configuration.Database/Infrastructure.Configuration.Database.csproj src/Infrastructure.Configuration.Database/
COPY src/Infrastructure.DataAccess/Infrastructure.DataAccess.csproj src/Infrastructure.DataAccess/
COPY src/Infrastructure.DataAccess.MongoDB/Infrastructure.DataAccess.MongoDB.csproj src/Infrastructure.DataAccess.MongoDB/
COPY src/Infrastructure.Logging/Infrastructure.Logging.csproj src/Infrastructure.Logging/
COPY src/Infrastructure.Telemetry/Infrastructure.Telemetry.csproj src/Infrastructure.Telemetry/
COPY apis/Infrastructure.Configuration.Api/Infrastructure.Configuration.Api.csproj apis/Infrastructure.Configuration.Api/

# Restore dependencies
RUN dotnet restore Infrastructure.sln

# Copy all source files
COPY src/ src/
COPY apis/ apis/

# Build and publish the API project
WORKDIR /src/apis/Infrastructure.Configuration.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Infrastructure.Configuration.Api.dll"]

