# BistroRater

## 🔐 Umgang mit Secrets (OIDC / OAuth Credentials)

Dieses Projekt verwendet für die Authentifizierung einen externen OpenID Connect (OIDC) Provider.
Aus Sicherheitsgründen werden keine Secrets im Repository abgelegt – weder in appsettings.json noch im Quellcode.

Stattdessen werden alle sicherheitsrelevanten Werte wie Authority, ClientId und ClientSecret ausschließlich über:

.NET User Secrets (für lokale Entwicklung)

Environment Variables (für Docker/Deployments)

oder einen Secret Store (z. B. Azure Key Vault)

bereitgestellt.

### Lokale Entwicklung (Empfohlen): .NET User Secrets

Im Projektordner einmalig initialisieren:

dotnet user-secrets init


Anschließend die benötigten OIDC-Werte setzen:

dotnet user-secrets set "Auth:Authority" "https://your-identity-provider"
dotnet user-secrets set "Auth:ClientId" "<client-id>"
dotnet user-secrets set "Auth:ClientSecret" "<client-secret>"


Die Werte werden außerhalb des Repositories gespeichert und nicht committed.

### Deployment / Docker: Environment Variables

Für Container oder produktive Umgebungen werden die OIDC-Parameter als Environment Variables gesetzt:

environment:
  - Auth__Authority=https://your-identity-provider
  - Auth__ClientId=<client-id>
  - Auth__ClientSecret=<client-secret>