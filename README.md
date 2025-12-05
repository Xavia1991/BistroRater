# BistroRater

## 🔐 Umgang mit Secrets (OIDC / OAuth Credentials)

Dieses Projekt verwendet für die Authentifizierung einen externen OpenID Connect (OIDC) Provider.
alle sicherheitsrelevanten Werte wie Authority, ClientId und ClientSecret ausschließlich über:

.NET User Secrets (für lokale Entwicklung)
Environment Variables (für Docker/Deployments)
oder einen Secret Store (z. B. Azure Key Vault) bereitgestellt.

### Lokale Entwicklung (Empfohlen): .NET User Secrets

Im Projektordner einmalig initialisieren:

dotnet user-secrets init


Anschließend die benötigten OIDC-Werte setzen:

```
dotnet user-secrets set "Auth:Authority" "https://your-identity-provider"
dotnet user-secrets set "Auth:ClientId" "<client-id>"
dotnet user-secrets set "Auth:ClientSecret" "<client-secret>"
```


Die Werte werden außerhalb des Repositories gespeichert und nicht committed.

### Deployment / Docker: Environment Variables

Für Container oder produktive Umgebungen werden die OIDC-Parameter als Environment Variables gesetzt:

environment:
```
  - Auth__Authority=https://your-identity-provider
  - Auth__ClientId=<client-id>
  - Auth__ClientSecret=<client-secret>
```

## 🐘 PostgreSQL Konfiguration

Die Anwendung nutzt standardmäßig PostgreSQL. In `appsettings.json` ist eine lokale Verbindung voreingestellt:

```
Host=localhost;Port=5432;Database=bistrorater;Username=bistrorater;Password=changeMe
```

Für Deployments werden die Werte idealerweise per Environment Variable überschrieben, z. B.:

```
ConnectionStrings__Default=Host=postgres;Port=5432;Database=bistrorater;Username=bistrorater;Password=superSecret
```

## 🐳 Docker Build & Run

Mit dem bereitgestellten `Dockerfile` lässt sich die Anwendung containerisieren (Multi-Stage Build auf dem aktuellen .NET SDK/Runtime):

```
docker build -t bistrorater:latest .
```

Anschließend kann der Container gestartet werden. Der HTTP-Endpunkt wird auf Port `8080` exponiert:

```
docker run \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__Default="Host=postgres;Port=5432;Database=bistrorater;Username=bistrorater;Password=superSecret" \
  -e Auth__Authority="https://your-identity-provider" \
  -e Auth__ClientId="<client-id>" \
  -e Auth__ClientSecret="<client-secret>" \
  -p 8080:8080 \
  bistrorater:latest
```

Die API-Base-URL (`Api:BaseUrl`) sollte auf den Host/Port des API-Backends zeigen und kann ebenfalls per Environment Variable gesetzt werden.
