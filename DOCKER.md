# Docker Deployment Guide

This guide explains how to deploy the Infrastructure Solution using Docker and Docker Compose.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Quick Start

### Build and Run All Services

From the project root directory:

```bash
docker-compose up --build
```

This will:
1. Build the API Docker image
2. Build the UI Docker image
3. Start MongoDB container
4. Start all services together

### Access the Services

- **UI**: http://localhost:4200
- **API**: http://localhost:8080
- **MongoDB**: localhost:27017

### Stop All Services

```bash
docker-compose down
```

To also remove volumes (including MongoDB data):

```bash
docker-compose down -v
```

## Individual Service Deployment

### Build API Only

```bash
docker build -f apis/Infrastructure.Configuration.Api/Dockerfile -t infrastructure-api:latest .
```

### Run API Container

```bash
docker run -d \
  --name infrastructure-api \
  -p 8080:8080 \
  -e MongoDb__ConnectionString=mongodb://host.docker.internal:27017 \
  -e MongoDb__DatabaseName=ConfigurationDb \
  infrastructure-api:latest
```

### Build UI Only

```bash
cd ui/Infrastructure.Configuration.UI
docker build -t infrastructure-ui:latest .
```

### Run UI Container

```bash
docker run -d \
  --name infrastructure-ui \
  -p 4200:80 \
  infrastructure-ui:latest
```

## Docker Compose Configuration

The `docker-compose.yml` file orchestrates all services:

- **mongodb**: MongoDB database with persistent volume
- **api**: .NET API service
- **ui**: Angular UI served by nginx

### Environment Variables

You can override environment variables in `docker-compose.yml` or create a `.env` file:

```env
MONGODB_CONNECTION_STRING=mongodb://mongodb:27017
MONGODB_DATABASE_NAME=ConfigurationDb
REGION=us-east-1
ASPNETCORE_ENVIRONMENT=Production
```

### Updating API URL in UI

The UI needs to know the API URL. Update `ui/Infrastructure.Configuration.UI/src/app/configuration.service.ts`:

```typescript
private apiUrl = 'http://localhost:8080/api';
```

Or use environment variables in the Angular app for different environments.

## Production Deployment

### Build for Production

```bash
docker-compose -f docker-compose.yml build
```

### Run in Detached Mode

```bash
docker-compose up -d
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f ui
docker-compose logs -f mongodb
```

### Scale Services (if needed)

```bash
docker-compose up -d --scale api=3
```

## Troubleshooting

### API Cannot Connect to MongoDB

If the API cannot connect to MongoDB:

1. Ensure MongoDB container is running: `docker-compose ps`
2. Check MongoDB logs: `docker-compose logs mongodb`
3. Verify connection string in `docker-compose.yml` uses `mongodb://mongodb:27017` (service name, not localhost)

### UI Cannot Connect to API

1. Check API is running: `docker-compose ps`
2. Verify API URL in UI configuration
3. Check CORS settings in API (should allow requests from UI origin)
4. View API logs: `docker-compose logs api`

### Port Conflicts

If ports 8080, 4200, or 27017 are already in use, update `docker-compose.yml`:

```yaml
ports:
  - "8081:8080"  # Change host port
```

### Rebuild After Code Changes

```bash
docker-compose up --build
```

### Clean Up

Remove all containers, networks, and volumes:

```bash
docker-compose down -v
docker system prune -a
```

## Health Checks

The services include health checks:

- MongoDB: Checks if database is responding
- API: Checks if HTTP endpoint is responding

View health status:

```bash
docker-compose ps
```

## Data Persistence

MongoDB data is persisted in a Docker volume named `mongodb-data`. This volume persists even if containers are removed.

To backup MongoDB data:

```bash
docker run --rm -v infrastructure-mongodb-data:/data -v $(pwd):/backup mongo tar czf /backup/mongodb-backup.tar.gz /data
```

To restore:

```bash
docker run --rm -v infrastructure-mongodb-data:/data -v $(pwd):/backup mongo tar xzf /backup/mongodb-backup.tar.gz -C /
```

## Security Considerations

For production:

1. Use environment variables for sensitive data (passwords, connection strings)
2. Enable MongoDB authentication
3. Use HTTPS for API and UI
4. Configure proper CORS policies
5. Use secrets management (Docker secrets, Kubernetes secrets, etc.)
6. Regularly update base images
7. Scan images for vulnerabilities: `docker scan infrastructure-api:latest`

## Multi-Environment Setup

Create separate docker-compose files:

- `docker-compose.dev.yml` - Development
- `docker-compose.prod.yml` - Production

Use with:

```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up
```

