# MongoDB Setup Guide

This guide explains how to set up and run MongoDB for the Infrastructure Solution using Docker.

## Prerequisites

- Docker Desktop installed and running
- Basic knowledge of Docker commands

## Quick Start

The fastest way to get MongoDB running:

```bash
docker run -d --name mongodb -p 27017:27017 mongo:latest
```

This starts MongoDB without authentication, suitable for local development.

## Detailed Setup

### Starting MongoDB Container

1. **Start Docker Desktop** (if not already running)

2. **Run MongoDB container:**
   ```bash
   docker run -d --name mongodb -p 27017:27017 mongo:latest
   ```
   
   This command:
   - Runs MongoDB in detached mode (`-d`)
   - Names the container `mongodb` (`--name mongodb`)
   - Maps port 27017 from container to host (`-p 27017:27017`)
   - Uses the latest MongoDB image (`mongo:latest`)

3. **Verify MongoDB is running:**
   ```bash
   docker ps
   ```
   
   You should see the `mongodb` container in the list with status "Up".

4. **Check MongoDB logs (optional):**
   ```bash
   docker logs mongodb
   ```

### Stopping MongoDB Container

To stop the MongoDB container:
```bash
docker stop mongodb
```

To stop and remove the container:
```bash
docker stop mongodb
docker rm mongodb
```

### Restarting MongoDB Container

If the container already exists but is stopped:
```bash
docker start mongodb
```

### Configuration

The default connection string for the API is:
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "ConfigurationDb"
  }
}
```

This is configured in `apis/Infrastructure.Configuration.Api/appsettings.json`.

## Troubleshooting

### Container Already Exists

If you get an error that the container name already exists:
```bash
docker rm -f mongodb
docker run -d --name mongodb -p 27017:27017 mongo:latest
```

### Port Already in Use

If port 27017 is already in use, you can use a different port:
```bash
docker run -d --name mongodb -p 27018:27017 mongo:latest
```

Then update `appsettings.json` to use `mongodb://localhost:27018`.

### Connection Issues

If you're experiencing connection issues:

1. **Ensure Docker Desktop is running:**
   ```bash
   docker ps
   ```

2. **Verify the container is running:**
   ```bash
   docker ps --filter "name=mongodb"
   ```

3. **Check container logs:**
   ```bash
   docker logs mongodb
   ```

4. **Ensure no other MongoDB instance is running on port 27017:**
   - Check if MongoDB is installed locally and running as a service
   - Check for other Docker containers using port 27017

5. **Restart the container:**
   ```bash
   docker restart mongodb
   ```

### Authentication Errors

If you see authentication errors like "Command find requires authentication":

1. **Stop and remove the existing container:**
   ```bash
   docker stop mongodb
   docker rm mongodb
   ```

2. **Start a new container without authentication:**
   ```bash
   docker run -d --name mongodb -p 27017:27017 mongo:latest
   ```

## MongoDB with Authentication (Optional)

For production-like setup with authentication:

### Setup with Authentication

1. **Start MongoDB with authentication:**
   ```bash
   docker run -d --name mongodb -p 27017:27017 \
     -e MONGO_INITDB_ROOT_USERNAME=admin \
     -e MONGO_INITDB_ROOT_PASSWORD=password \
     mongo:latest
   ```

2. **Update connection string in `appsettings.json`:**
   ```json
   {
     "MongoDb": {
       "ConnectionString": "mongodb://admin:password@localhost:27017",
       "DatabaseName": "ConfigurationDb"
     }
   }
   ```

### Creating Database Users

For better security, create specific database users instead of using the root user:

1. **Connect to MongoDB:**
   ```bash
   docker exec -it mongodb mongosh
   ```

2. **Switch to the ConfigurationDb database:**
   ```javascript
   use ConfigurationDb
   ```

3. **Create a user:**
   ```javascript
   db.createUser({
     user: "configuser",
     pwd: "configpassword",
     roles: [{ role: "readWrite", db: "ConfigurationDb" }]
   })
   ```

4. **Update connection string:**
   ```json
   {
     "MongoDb": {
       "ConnectionString": "mongodb://configuser:configpassword@localhost:27017/ConfigurationDb",
       "DatabaseName": "ConfigurationDb"
     }
   }
   ```

## Data Persistence

By default, data stored in the MongoDB container is ephemeral and will be lost when the container is removed. To persist data:

1. **Create a Docker volume:**
   ```bash
   docker volume create mongodb-data
   ```

2. **Run MongoDB with volume:**
   ```bash
   docker run -d --name mongodb -p 27017:27017 \
     -v mongodb-data:/data/db \
     mongo:latest
   ```

3. **Data will persist even if the container is removed:**
   ```bash
   docker stop mongodb
   docker rm mongodb
   # Data is still in the volume
   docker run -d --name mongodb -p 27017:27017 \
     -v mongodb-data:/data/db \
     mongo:latest
   ```

## Alternative: MongoDB Atlas (Cloud)

For cloud-hosted MongoDB:

1. Sign up for free at https://www.mongodb.com/cloud/atlas
2. Create a free cluster
3. Get your connection string
4. Update `appsettings.json`:
   ```json
   {
     "MongoDb": {
       "ConnectionString": "mongodb+srv://username:password@cluster.mongodb.net/",
       "DatabaseName": "ConfigurationDb"
     }
   }
   ```

## Useful Commands

### View Running Containers
```bash
docker ps
```

### View All Containers (including stopped)
```bash
docker ps -a
```

### View Container Logs
```bash
docker logs mongodb
docker logs -f mongodb  # Follow logs in real-time
```

### Execute Commands in Container
```bash
docker exec -it mongodb mongosh
```

### Remove Container and Volume
```bash
docker stop mongodb
docker rm mongodb
docker volume rm mongodb-data  # If using volumes
```

