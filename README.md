# BistroRater – Deployment- & Entwicklungsanleitung

Diese Anleitung beschreibt alle Schritte, um die BistroRater‑Webanwendung sowohl **lokal** als auch **in Docker** (inkl. PostgreSQL & Auth0 Login) erfolgreich auszuführen.

Sie umfasst:

- Lokales Setup
- Umgang mit Secrets
- Verwendung einer `.env` Datei
- Docker & Docker‑Compose Konfiguration
- Start & Troubleshooting

---

# 1. Voraussetzungen

- .NET 9 oder .NET 10 SDK
- Docker Desktop (aktuellste Version)
- PostgreSQL (lokal optional, über Docker empfohlen)
- Auth0 (oder anderer OIDC‑Provider)

---

# 2. Secrets konfigurieren

Die Anwendung benötigt OAuth‑/OIDC‑Daten (Authority, ClientId, ClientSecret).

Dafür gibt es **zwei mögliche Wege**:

---

## OPTION A – Lokale Entwicklung mit `dotnet user-secrets`

Im Projektordner `BistroRater`:

```
dotnet user-secrets init
dotnet user-secrets set "Auth:Authority" "https://DEIN_AUTH0_DOMAIN/"
dotnet user-secrets set "Auth:ClientId" "DEINE_CLIENT_ID"
dotnet user-secrets set "Auth:ClientSecret" "DEIN_CLIENT_SECRET"
```

Diese Daten gelten nur lokal.

---

## OPTION B – Mit `.env` Datei (für Docker empfohlen)

Lege eine Datei `.env` neben deine `docker-compose.yml`:

```
# ===== Auth0 Daten =====
AUTH_AUTHORITY=https://dev-xxxxxx.us.auth0.com/
AUTH_CLIENTID=DEINE_CLIENT_ID
AUTH_CLIENTSECRET=DEIN_CLIENT_SECRET

# ===== PostgreSQL =====
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=bistrodb
```

Docker Compose lädt diese Datei **automatisch**, ohne dass du etwas referenzieren musst.

---

# 3. Docker Compose Setup

Deine `docker-compose.yml` muss Folgendes enthalten:

- Postgres‑Container mit Healthcheck  
- WebApp‑Container mit Environment‑Variablen  
- Persistente DataProtection‑Keys (Pflicht für Auth0 & Cookies)  
- Interne BaseUrl der API (`http://webapp:8080`)

Beispielkonfiguration:

```yaml
services:
  postgres:
    image: postgres:16
    container_name: bistro-postgres
    restart: always
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: ${POSTGRES_DB}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 3s
      timeout: 3s
      retries: 5

  webapp:
    build: .
    container_name: bistro-webapp
    restart: always
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      RUNNING_IN_DOCKER: "true"

      Api__BaseUrl: "http://webapp:8080"

      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"

      Auth__Authority: "${AUTH_AUTHORITY}"
      Auth__ClientId: "${AUTH_CLIENTID}"
      Auth__ClientSecret: "${AUTH_CLIENTSECRET}"

    ports:
      - "7015:8080"

    volumes:
      - dataprotection-keys:/root/.aspnet/DataProtection-Keys

volumes:
  postgres-data:
  dataprotection-keys:
```

---

# 4. Docker Build & Start

Benutze die **docker-starter.bat** Datei oder führe folgende Befehle im Terminal aus:
```
docker compose build
docker compose up
```

Danach im Browser öffnen:

```
http://localhost:7015
```

Funktionen, die nun laufen sollten:

- Login über Auth0
- Tages- & Wochenmenüs laden
- Bewertungen abgeben
- Autocomplete-Vorschläge  
- Top Meals anzeigen

---

# 5. Lokale Entwicklung ohne Docker (optional)

Wenn du die App **lokal** starten willst, aber Postgres weiter über Docker laufen lässt:

```
docker compose up postgres
```

In `appsettings.Development.json`:

```
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=bistrodb;Username=postgres;Password=postgres"
}
```

Dann:

```
dotnet run
```



# 6. Autor

Christian Otting – BistroRater Demo Projekt
