# BistroRater – Internal Bistro Meal Rating System

BistroRater is a small internal web application that allows employees to rate daily bistro meals.  
It is built with **ASP.NET Core 10**, **EF Core 10**, **Blazor Server**, **PostgreSQL**, and **OIDC (Auth0)**.

The application is fully containerized using **Docker Compose** and runs with:

- WebApp (ASP.NET Core)
- PostgreSQL database
- Adminer (optional DB UI)

---

## ✨ Features

- **Daily Menu Management**: Up to 3 meal options per day (Grill Sandwiches, Smuts Leibspeise, Just Good Food)
- **Rating System**: Users can rate today's meals with 1-5 stars
- **Meal Naming**: All users can edit/update meal descriptions
- **Autocomplete**: Smart suggestions from previously entered meal names
- **Top Meals View**: See the best-rated meals based on average ratings
- **OIDC Authentication**: Secure login via Auth0 (optional)
- **Development Mode**: Run without authentication for testing
- **Automatic Migrations**: Database schema updates on startup

---

## ⚙️ Technology Stack

- **.NET 10 / ASP.NET Core**
- **Blazor Server**
- **Entity Framework Core 10**
- **PostgreSQL 16**
- **Docker / Docker Compose**
- **Auth0 (OpenID Connect Login)**
- **Serilog** for structured logging
- **xUnit** for unit testing
- **Adminer** for database inspection

---

## 🚀 Quick Start

### Running with Docker (Recommended)

1. Copy the example environment file:

```bash
cp .env.example .env
```

2. Insert your Auth0 credentials into `.env`.

3. Start the full stack:

```bash
docker compose up --build
```

4. Open the application:

```
http://localhost:7015
```

5. (Optional) Open the Adminer UI:

```
http://localhost:8080
```

### Running Locally (Development)

1. Ensure PostgreSQL is running locally or update connection string in `appsettings.Development.json`

2. Run the application:

```bash
cd BistroRater
dotnet run
```

3. Navigate to dev-login to access without authentication:

```
https://localhost:7015/dev-login
```

### Running Tests

Run all unit tests:

```bash
cd BistroRater.Tests
dotnet test
```

Or run tests for the entire solution:

```bash
dotnet test BistroRater.slnx
```

---

## 🐳 Docker Setup

### `docker-compose.yml`

```yaml
services:
  postgres:
    image: postgres:16
    container_name: bistro-postgres
    restart: always
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

  webapp:
    build: .
    container_name: bistro-webapp
    restart: always
    depends_on:
      - postgres
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

      Auth__Authority: "${AUTH_AUTHORITY}"
      Auth__ClientId: "${AUTH_CLIENTID}"
      Auth__ClientSecret: "${AUTH_CLIENTSECRET}"
    ports:
      - "7015:8080"

  adminer:
    image: adminer
    container_name: bistro-adminer
    restart: always
    ports:
      - "8080:8080"

volumes:
  postgres-data:
```

---

## 🔐 Environment Variables

### `.env.example`

```env
# ==== Auth0 Configuration ====
AUTH_AUTHORITY=https://YOUR_DOMAIN.eu.auth0.com/
AUTH_CLIENTID=YOUR_CLIENT_ID
AUTH_CLIENTSECRET=YOUR_CLIENT_SECRET

# ==== PostgreSQL Configuration ====
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=bistrodb
```

Add `.env` to `.gitignore`:

```
.env
```

---

## 🔑 Auth0 Setup

### Allowed Callback URLs

```
https://localhost:7015/signin-oidc
```

### Allowed Logout URLs

```
https://localhost:7015/
```

### Allowed Web Origins

```
https://localhost:7015
```

### API Identifier (if used)

```
api://bistrorater
```

---

## 🧱 Database Migrations

Migrations are automatically applied at startup:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BistroContext>();
    db.Database.Migrate();
}
```

---

## 📝 Logging

The application uses **Serilog** for structured logging. Logs are written to:

- **Console** (stdout) - for Docker/containerized environments
- **File** - `logs/bistrorater-{Date}.log` with daily rolling

Configure log levels in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

---

## 🧪 Testing

The project includes comprehensive unit tests covering:

- Menu management endpoints
- Rating functionality  
- Autocomplete features
- Top meals retrieval

Run tests with:

```bash
dotnet test
```

Test coverage includes:
- **MenuController**: Weekly menu creation, renaming, autocomplete
- **RatingsController**: Rating creation/updates, top meals calculation

---

## 🔌 REST API Endpoints

### Menu Management

- `GET /api/menu/week?date={date}` - Get weekly menu (Monday-Friday)
- `POST /api/menu/rename` - Update meal description
- `GET /api/menu/autocomplete?query={query}` - Get meal name suggestions

### Ratings

- `POST /api/ratings/rate` - Rate a meal (1-5 stars, today only)
- `GET /api/ratings/top?minRatings={min}` - Get top-rated meals

All endpoints return JSON and follow RESTful conventions.

---

## 👨‍💻 Developer Mode (No Login Required)

```json
{
  "Auth": {
    "RequireAuth": false
  }
}
```

This injects a fake user during development.

---

## 🧭 Architecture Overview

```
+-------------------+       +------------------+
|   WebApp (8080)   | --->  | PostgreSQL (5432)|
| ASP.NET + Blazor  |       | Data Persistence |
+-------------------+       +------------------+
           |
           | optional
           v
+-------------------+
|  Adminer (8080)   |
| DB Visualization  |
+-------------------+
```

---

## ❗ Troubleshooting

### OAuth not configured
Check environment variables inside the container:

```bash
docker exec -it bistro-webapp printenv | grep Auth
```

### PostgreSQL connection issues

```bash
docker logs bistro-postgres
```

---

## 📄 License

Internal assignment – not intended for public distribution.
