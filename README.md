# Infrastructure Solution

Enterprise-grade infrastructure solution with multi-tenancy at its core, providing comprehensive support for configuration management, logging, telemetry, and data access.

## Solution Structure

### Core Infrastructure Projects

1. **Infrastructure.MultiTenancy** - Core multi-tenancy support
   - Tenant context management using AsyncLocal
   - ASP.NET Core middleware for tenant extraction
   - Tenant resolver interface for database connection configuration

2. **Infrastructure.Configuration** - Hierarchical configuration management
   - Multi-level configuration resolution (User → Tenant → Region → Environment → Global)
   - Feature flag support with percentage rollouts and targeting rules
   - Caching support for performance

3. **Infrastructure.Logging** - OpenTelemetry logging infrastructure
   - Automatic tenant context enrichment
   - Console and OTLP exporters
   - Structured logging support

4. **Infrastructure.Telemetry** - OpenTelemetry metrics and tracing
   - ActivitySource and Meter factories
   - Tenant-aware span enrichment
   - Baggage propagation for distributed tracing

5. **Infrastructure.DataAccess** - Abstracted data access layer
   - Database-agnostic interface
   - Factory pattern for tenant-specific connections
   - Follows Dependency Inversion Principle

6. **Infrastructure.DataAccess.MongoDB** - MongoDB implementation
   - Full CRUD operations
   - Tenant-aware connection management
   - OpenTelemetry instrumentation

7. **Infrastructure.Configuration.Database** - Shared database models
   - MongoDB document models for configurations and feature flags
   - Repository implementations

### API Projects

8. **Infrastructure.Configuration.Api** - Configuration management API
   - RESTful API for configurations and feature flags
   - SignalR hub for real-time configuration updates
   - WebSocket pub/sub architecture

### UI Projects

9. **Infrastructure.Configuration.UI** - Angular web application
   - Configuration management interface
   - Feature flag management
   - Real-time updates via SignalR

## Key Features

### Multi-Tenancy
- Tenant context propagation through AsyncLocal
- Automatic tenant extraction from HTTP requests (header, subdomain, path, JWT claims)
- Tenant-specific database connection configuration
- Tenant-aware logging and telemetry

### Configuration Management
- Hierarchical configuration resolution
- Support for multiple scopes (Global, Environment, Region, Tenant, User)
- Caching with configurable expiration
- Real-time updates via WebSocket

### Feature Flags
- Percentage-based rollouts
- Targeting rules (equals, contains, startsWith, etc.)
- A/B testing support with variants
- Hierarchical resolution like configurations

### Logging & Telemetry
- OpenTelemetry integration
- Automatic tenant context enrichment
- Distributed tracing support
- Metrics collection

### Data Access
- Abstracted interface (can swap MongoDB for other databases)
- Tenant-specific connection management
- Factory pattern for extensibility

## SOLID Principles

This solution follows all SOLID principles:

- **Single Responsibility**: Each project has one clear purpose
- **Open/Closed**: Extensible through interfaces and abstractions
- **Liskov Substitution**: Implementations can be swapped (e.g., different databases)
- **Interface Segregation**: Small, focused interfaces
- **Dependency Inversion**: Depend on abstractions, not concretions

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- MongoDB (for data storage) - See [MongoDB Setup Guide](MONGODB.md)
- Node.js and npm (for Angular UI)
- Docker Desktop (for running MongoDB in a container or full deployment)

### Docker Deployment

For containerized deployment, see the [Docker Deployment Guide](DOCKER.md). This includes:
- Dockerfiles for API and UI
- Docker Compose configuration
- Production deployment instructions

### Observability and Logging

For centralized logging, metrics, and tracing, see the [Logging and Observability Guide](LOGGING.md). This includes:
- OpenTelemetry Collector setup
- Grafana, Loki, Prometheus, and Tempo integration
- Query examples and dashboard configuration

### MongoDB Setup

Before running the API, you need to set up MongoDB. See the [MongoDB Setup Guide](MONGODB.md) for detailed instructions on:
- Starting MongoDB with Docker
- Configuration and connection strings
- Troubleshooting common issues
- Setting up authentication
- Data persistence options

### Configuration API Setup

1. Update `appsettings.json` in `Infrastructure.Configuration.Api`:
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ConfigurationDb"
  },
  "Region": "us-east-1"
}
```

2. Run the API:
```bash
cd apis/Infrastructure.Configuration.Api
dotnet run
```

### Angular UI Setup

1. Install dependencies:
```bash
cd ui/Infrastructure.Configuration.UI
npm install
```

2. Update API URL in `src/app/configuration.service.ts` if needed

3. Start the development server:
```bash
npm start
```

## Architecture

### Multi-Tenancy Flow

1. Request arrives → TenantMiddleware extracts tenant ID
2. TenantResolver resolves full tenant context (including DB connection)
3. TenantContextAccessor sets tenant context (AsyncLocal)
4. All subsequent operations (logging, data access, config) use tenant context
5. Context cleared after request completes

### Configuration Resolution

Configuration values are resolved in order of specificity:
1. User (most specific)
2. Tenant
3. Region
4. Environment
5. Global (least specific)

The first match wins.

### Real-Time Updates

1. Configuration/Feature flag changed via API
2. ConfigurationNotificationService broadcasts via SignalR
3. Connected clients receive update notification
4. Clients refresh their local cache

## Extensibility

### Adding a New Database

1. Create new project: `Infrastructure.DataAccess.{DatabaseName}`
2. Implement `IDataAccess` interface
3. Create factory implementation
4. Register in DI container
5. Business code remains unchanged!

### Adding New Configuration Scopes

1. Extend `ConfigurationScope` enum
2. Update resolution logic in `ConfigurationService`
3. Update UI to support new scope

## Best Practices

1. Always use `ITenantContextAccessor` to get current tenant (never pass tenant ID as parameter)
2. Use `IDataAccessFactory` to get data access instances (ensures tenant-specific connections)
3. Leverage hierarchical configuration (set defaults at Global, override at Tenant)
4. Use feature flags for gradual rollouts
5. Monitor telemetry with tenant context for per-tenant insights

## License

This is an enterprise infrastructure solution designed for use as building blocks in multiple projects.

