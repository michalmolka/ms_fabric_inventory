# Power BI / Fabric Metadata Portal (Blazor)

A Blazor Server portal for browsing Microsoft Fabric / Power BI objects (capacities, workspaces, datasets, reports, lakehouses, warehouses), refreshing metadata from REST API, and analyzing Event Log anomalies.

## Key Features

- Entra ID sign-in (only authenticated users can access the app).
- Fabric object inventory pages:
  - Capacities
  - Workspaces
  - Datasets
  - Reports
  - Lakehouses
  - Warehouses
- Detail pages for core objects (opened from table IDs).
- SQL cache with soft-delete (`IsActive`) and refresh metadata.
- Dataset refresh actions + last refresh status/time.
- Event Log page with:
  - custom KQL query execution,
  - anomaly detection,
  - trend chart with average line,
  - KPI cards (min/max/avg/median/total/anomaly count/rate),
  - anomaly results table.
- SignalR notifications (real-time UI updates).

## Tech Stack

- .NET / ASP.NET Core / Blazor Server
- Entity Framework Core + SQL Server
- Microsoft Identity Web (Entra ID / OpenID Connect)
- Power BI REST API
- Microsoft Fabric REST API
- Kusto (Event Log query support)
- ML.NET (anomaly detection)

## Architecture

High-level architecture is documented here:

- [docs/architecture.md](docs/architecture.md)
- [docs/project-documentation.md](docs/project-documentation.md)

## Configuration

Copy `.env.example` to `.env` and fill values:

```bash
cp .env.example .env
```

Required environment variables:

```env
ConnectionStrings__blz0ContextSql=Server=host.docker.internal,1433;Database=FabricDB;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True;Encrypt=True;

ApiCredentials__tenantId=YOUR_TENANT_ID
ApiCredentials__clientId=YOUR_APP_CLIENT_ID
ApiCredentials__clientSecret=YOUR_APP_CLIENT_SECRET

PowerBiServicePrincipal__tenantId=YOUR_TENANT_ID
PowerBiServicePrincipal__clientId=YOUR_POWER_BI_SP_CLIENT_ID
PowerBiServicePrincipal__clientSecret=YOUR_POWER_BI_SP_CLIENT_SECRET
```

Optional Event Log/Kusto:

```env
Kusto__clusterUri=...
Kusto__kqlDatabase=...
```

### Entra ID Settings

The app reads `AzureAd:*` settings.
If `AzureAd:TenantId/ClientId/ClientSecret` are missing, `Program.cs` maps them from `ApiCredentials:*`.

In Entra App Registration make sure:

- Redirect URI includes `https://localhost:<https-port>/signin-oidc`
- Logout URI includes `https://localhost:<https-port>/signout-callback-oidc`
- ID tokens are enabled in authentication settings
- A client secret is created and valid
- Delegated API permissions include the scopes used by the app:
  - Power BI API permissions behind `https://analysis.windows.net/powerbi/api/.default`
  - Fabric REST: `Workspace.Read.All`
  - Fabric Lakehouse tables: `Lakehouse.Read.All`
  - Fabric Warehouse SQL endpoint: Azure SQL Database delegated access for `https://database.windows.net/.default`

Lakehouse and warehouse list/details use Fabric REST workspace item endpoints. Some Power BI workspaces do not support Fabric item APIs; those return `WorkspaceTypeNotSupported` and are skipped during global refresh.

## Local Development (Dev Container)


0. Run the application

Terminal: dotnet run --launch-profile https
Browser: https://localhost:7040

1. Start SQL Server (example):

```bash
docker run \
  --platform linux/amd64 \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=CHANGE_ME" \
  -p 1433:1433 \
  --name blz0-sqlserver \
  -d mcr.microsoft.com/mssql/server:2025-latest
```

One-line equivalent:

```bash
docker run --platform linux/amd64 -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=CHANGE_ME' -p 1433:1433 --name blz0-sqlserver -d mcr.microsoft.com/mssql/server:2025-latest
```

2. Open project in VS Code Dev Container.
3. Create and fill `.env`.
4. Apply migrations:

```bash
dotnet ef database update --project blz0/blz0
```

5. Run app:

```bash
dotnet run --project blz0/blz0
```

### Local static assets

Bootstrap is kept locally in `blz0/blz0/wwwroot/lib/bootstrap` as a fallback for Docker/dev environments with unreliable CDN access. Bootstrap Icons are restored locally through `blz0/blz0/libman.json` and loaded from `wwwroot/lib/bootstrap-icons`.

If frontend library assets are missing, restore them from inside `blz0/blz0`:

```bash
libman restore
```

## Troubleshooting

### `IDW10106: The 'ClientId' option must be provided`

- Missing `AzureAd:ClientId` (or missing `ApiCredentials__clientId` in `.env`).

### `No DefaultChallengeScheme found`

- Authentication is not configured correctly in `Program.cs`.
- This repository already sets default schemes for Cookies + OpenIdConnect.

### `Correlation failed`

- Commonly caused by HTTP/HTTPS mismatch or invalid callback URLs.
- Ensure app URL and Entra redirect URIs use the same scheme/port.

### HTTPS dev cert error

- In container, generate certificate and configure Kestrel profile/cert paths.
- If unavailable, use HTTP profile for local checks (sign-in flow typically needs HTTPS).

### SQL migration error (missing columns)

Example:

- `Invalid column name 'LastRefreshAtUtc'`
- `Invalid object name 'Lakehouse'`

Fix:

```bash
dotnet ef database update --project blz0/blz0
```

## Security Notes

- Do not commit real secrets (`.env` is local-only).
- Rotate client secrets regularly.
- Restrict Entra app access to required users/groups.

## GitHub "About" (ready to paste)

Short description:

`Blazor Server portal for Microsoft Fabric/Power BI metadata: Entra ID auth, SQL cache, lakehouse/warehouse inventory, dataset refresh tracking, Event Log KQL, anomaly detection, and object detail pages.`

Suggested topics:

`blazor` `aspnet-core` `powerbi` `microsoft-fabric` `entra-id` `entity-framework-core` `sql-server` `signalr` `mlnet` `kusto`
