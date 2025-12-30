# ProjectService

## Overview

The **ProjectService** is a RESTful API microservice within the Forget Me Not (FMN) application ecosystem that manages organizational containers for grouping related habits and tasks. This service helps users organize their activities into meaningful collections with color-coding, status tracking, and project-level analytics.

### Key Features

- Create and manage projects with customizable properties
- Group habits and tasks into organized collections
- Color-coded projects for visual organization
- Status tracking (Active, Archived, Completed)
- User-specific project isolation with authentication
- RESTful API with OAuth2 authentication via Keycloak
- PostgreSQL database for reliable data persistence
- API versioning support
- Swagger/OpenAPI documentation

## Prerequisites

Before running the ProjectService, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Docker](https://www.docker.com/get-started) and [Docker Compose](https://docs.docker.com/compose/install/)
- [PostgreSQL](https://www.postgresql.org/download/) (if running locally without Docker)
- Access to Keycloak authentication server
- `.env` file with required environment variables (obtain from @wtvamp or @lunarjuice)

## Architecture & Technology Stack

### Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Language**: C# 11
- **Database**: PostgreSQL 15+
- **ORM**: Entity Framework Core with Npgsql provider
- **Authentication**: JWT Bearer tokens via Keycloak OAuth2
- **API Documentation**: Swagger/OpenAPI 3.0
- **Containerization**: Docker & Docker Compose
- **API Versioning**: Query string-based versioning

### Architecture

The ProjectService follows a layered architecture pattern:

```
┌─────────────────────────────────────┐
│    Controllers (API Endpoints)      │
│   - ProjectsController (v0.1)       │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│       Business Logic Layer          │
│   - Project Management              │
│   - Status Tracking                 │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      Data Access Layer (EF Core)    │
│   - ProjectContext                  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      PostgreSQL Database            │
│   - Projects Table                  │
│   - Migrations                      │
└─────────────────────────────────────┘
```

## Getting Started

### Environment Variables

The ProjectService requires the following environment variables. These should be configured in a `.env` file (for Docker Compose) or set in your environment (for `dotnet run`).

| Variable | Description | Example Value |
|----------|-------------|---------------|
| `Keycloak__Authority` | Keycloak server authority URL for JWT validation | `https://keycloak.example.com/realms/fmn` |
| `Keycloak__Audience` | Expected audience claim in JWT tokens | `fmn-api` |
| `Keycloak__OAuthClientId` | OAuth2 client ID for Swagger UI authentication | `fmn-swagger-client` |
| `Keycloak__AuthClientSecret` | OAuth2 client secret for Swagger UI | `your-client-secret` |
| `ConnectionString__DefaultConnection` | PostgreSQL connection string | `Host=projectdb;Database=projects;Username=postgres;Password=yourpassword` |
| `POSTGRES_PASSWORD` | PostgreSQL database password (Docker only) | `yourpassword` |
| `POSTGRES_DB` | PostgreSQL database name (Docker only) | `projects` |

**IMPORTANT**: Never commit `.env` files to source control. Obtain the appropriate `.env` file from your team lead.

### Setup Instructions

#### Option 1: Docker Compose Setup (Recommended)

Docker Compose is the **recommended** approach as it handles both the API and database automatically.

**Advantages:**
- Complete environment setup with one command
- Database automatically configured and health-checked
- Closer to production deployment environment
- Isolated from other services
- No manual database setup required

**Setup Steps:**

1. **Obtain the environment file:**
   ```bash
   # Request .env file from @wtvamp or @lunarjuice
   # Place it in the ProjectService directory
   ```

2. **Navigate to the service directory:**
   ```bash
   cd /path/to/ProjectService
   ```

3. **Start the service:**
   ```bash
   # Development environment (default)
   docker compose up -d

   # View logs
   docker compose logs -f projectservice

   # Stop the service
   docker compose down
   ```

4. **Verify the service:**
   - API: http://localhost:87
   - Swagger UI: http://localhost:87/swagger
   - Database: localhost:5440

#### Option 2: dotnet run Setup

The `dotnet run` approach provides faster iteration during development but **requires the database to be running first**.

**Advantages:**
- Faster code iteration (no container rebuilds)
- Easier debugging with IDE integration
- Direct access to application logs
- Immediate code changes without rebuild

**Disadvantages:**
- Requires manual database setup
- Database must be running via Docker Compose first
- Connection string must point to Docker database
- Less representative of production environment

**Setup Steps:**

1. **Start ONLY the database via Docker Compose:**
   ```bash
   cd /path/to/ProjectService
   docker compose up -d projectdb
   ```

2. **Verify database is healthy:**
   ```bash
   docker compose ps
   # Wait until projectdb shows "healthy" status
   ```

3. **Set environment variables:**
   ```bash
   # Windows (PowerShell)
   $env:Keycloak__Authority="https://keycloak.example.com/realms/fmn"
   $env:Keycloak__Audience="fmn-api"
   $env:Keycloak__OAuthClientId="fmn-swagger-client"
   $env:Keycloak__AuthClientSecret="your-client-secret"
   $env:ConnectionString__DefaultConnection="Host=localhost;Port=5440;Database=projects;Username=postgres;Password=yourpassword"

   # Alternatively, create a .env file and the application will load it automatically
   ```

4. **Run the service:**
   ```bash
   cd /path/to/ProjectService
   dotnet run
   ```

5. **Access the service:**
   - HTTPS: https://localhost:7255
   - HTTP: http://localhost:5151
   - Swagger UI: https://localhost:7255/swagger

#### Option 3: dotnet build

To build the service without running it:

```bash
cd /path/to/ProjectService
dotnet build
```

For release configuration:
```bash
dotnet build --configuration Release
```

## Running the Service

### Using Docker Compose

#### Development Environment
```bash
# Start services
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down

# Rebuild and start
docker compose up -d --build
```

#### QA Environment
```bash
# Create QA network and volume (first time only)
docker network create forgetmenot_qa
docker volume create project_db_qa_storage

# Start QA environment
docker compose -f docker-compose.yml -f docker-compose.qa.yml --env-file .env.qa up -d

# Access at http://localhost:287

# Stop QA environment
docker compose -f docker-compose.yml -f docker-compose.qa.yml --env-file .env.qa down
```

#### Production Environment
```bash
# Create production network and volume (first time only)
docker network create forgetmenot
docker volume create project_db_storage

# Start production environment
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env.prod up -d

# Stop production environment
docker compose -f docker-compose.yml -f docker-compose.prod.yml --env-file .env.prod down
```

### Using dotnet run

```bash
# Ensure database is running first
docker compose up -d projectdb

# Run the application
dotnet run

# Run with specific environment
dotnet run --environment Production

# Run and watch for changes (hot reload)
dotnet watch run
```

### Accessing Swagger UI

The ProjectService provides Swagger UI with support for multiple server environments:

| Environment | URL | Description | When to Use |
|-------------|-----|-------------|-------------|
| **Local dotnet run (HTTPS)** | https://localhost:7255 | Local development with `dotnet run` | Active development, debugging |
| **Local dotnet run (HTTP)** | http://localhost:5151 | Local development without SSL | Testing without certificates |
| **Local Docker Compose** | http://localhost:87 | Containerized local environment | Testing containerization |
| **Local Kubernetes** | https://localhost/project | Local K8s cluster | Testing Kubernetes deployment |
| **Cloud QA Server** | https://forgetmenotqa.uplifttech.org/project | QA environment | Integration testing |
| **Cloud Prod Server** | https://forgetmenot.uplifttech.org/project | Production environment | Production use |

**Accessing Swagger:**

1. Navigate to the appropriate URL + `/swagger`:
   - Development: https://localhost:7255/swagger
   - Docker Compose: http://localhost:87/swagger

2. **Authenticate with OAuth2:**
   - Click the "Authorize" button
   - Click "Authorize" in the oauth2 dialog
   - Login with your Keycloak credentials
   - Swagger will receive the JWT token automatically

3. **Select the server:**
   - Use the "Servers" dropdown at the top to switch between environments
   - Ensures API calls go to the correct backend

## Development Workflow

### Database Migrations

The ProjectService uses Entity Framework Core migrations:

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations (automatic on startup)
# Migrations run automatically when the service starts

# Rollback migration
dotnet ef migrations remove

# View migration history
dotnet ef migrations list
```

**Note**: Migrations are applied automatically on service startup via the `Program.cs` initialization code.

## Troubleshooting

### Common Issues

#### Issue: Service won't start - "Cannot connect to database"

**Solution:**
- Ensure database is running: `docker compose ps`
- Wait for database health check: Look for "healthy" status
- Verify connection string in environment variables
- Check database logs: `docker compose logs projectdb`

```bash
# Restart just the database
docker compose restart projectdb

# View database logs
docker compose logs projectdb
```

#### Issue: Swagger UI shows "Authorization failed"

**Solution:**
- Verify Keycloak environment variables are set correctly
- Check that your Keycloak user has appropriate permissions
- Ensure the OAuth client is configured in Keycloak
- Check browser console for detailed error messages

#### Issue: "Port already in use" error

**Solution:**
```bash
# For Docker Compose (port 87)
# Find and stop conflicting container
docker ps
docker stop <container-id>

# For dotnet run (ports 5151/7255)
# Windows PowerShell - Find process using port
netstat -ano | findstr :5151
taskkill /PID <process-id> /F

# Linux/Mac - Find and kill process
lsof -ti:5151 | xargs kill -9
```

### Debugging Tips

1. **Check service logs:**
   ```bash
   # Docker Compose
   docker compose logs -f projectservice

   # dotnet run - logs appear in console
   ```

2. **Verify environment variables:**
   ```bash
   # Inside Docker container
   docker compose exec projectservice env
   ```

3. **Test database connection:**
   ```bash
   # Connect to database directly
   docker compose exec projectdb psql -U postgres -d projects

   # List tables
   \dt

   # Exit
   \q
   ```

4. **Health check:**
   ```bash
   # Check if service is responding
   curl http://localhost:87/swagger/index.html
   ```

## API Documentation

### Authentication

All endpoints (except Swagger UI) require JWT Bearer token authentication via Keycloak.

### API Versioning

The ProjectService uses query string-based API versioning:

- Default version: `v0.1`
- Format: `?api-version=0.1`

### Example Endpoints

#### Get All Projects
```bash
curl -X GET "http://localhost:87/api/Projects?api-version=0.1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Get Project by ID
```bash
curl -X GET "http://localhost:87/api/Projects/123?api-version=0.1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Create New Project
```bash
curl -X POST "http://localhost:87/api/Projects?api-version=0.1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Health & Fitness",
    "description": "Track all health-related habits and tasks",
    "color": "#4CAF50",
    "status": "Active",
    "userId": "user-123"
  }'
```

#### Update Project
```bash
curl -X PUT "http://localhost:87/api/Projects/123?api-version=0.1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 123,
    "name": "Health & Fitness - Updated",
    "description": "Updated description",
    "color": "#2196F3",
    "status": "Active",
    "userId": "user-123"
  }'
```

#### Delete Project
```bash
curl -X DELETE "http://localhost:87/api/Projects/123?api-version=0.1" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Response Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content (successful deletion) |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing or invalid token) |
| 403 | Forbidden (insufficient permissions) |
| 404 | Not Found |
| 500 | Internal Server Error |

### Swagger Documentation

For complete API documentation with request/response schemas, visit the Swagger UI:
- Development: https://localhost:7255/swagger
- Docker Compose: http://localhost:87/swagger

The Swagger UI provides:
- Interactive API testing
- Request/response schemas
- OAuth2 authentication flow
- Example values
- Model definitions

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [Swagger/OpenAPI Specification](https://swagger.io/specification/)

## Support

For questions or issues:
- Contact: @wtvamp or @lunarjuice
- Check service logs for detailed error messages
- Review Keycloak configuration for authentication issues
- Verify database connectivity and migrations
