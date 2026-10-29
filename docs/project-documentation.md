# Power BI / Fabric Inventory Portal – Project Documentation

## 1. Application Purpose

The application is designed to browse and monitor Microsoft Fabric / Power BI assets, including:

- Capacities
- Workspaces
- Datasets
- Reports
- Lakehouses
- Warehouses

### Business Objectives

- Provide users with a simple and centralized view of their Power BI inventory.
- Reduce the number of Power BI REST API calls by using a SQL-based cache.
- Enable Event Log analysis, including KQL querying, statistics, and anomaly detection.

---

## 2. How the Application Works

1. Users authenticate through Microsoft Entra ID.
2. The UI primarily reads data from the SQL cache.
3. Refresh operations retrieve the latest state from Microsoft Fabric / Power BI and store it in SQL Server.
4. Missing objects are not physically deleted; instead, they are marked as `IsActive = false`.
5. The Event Log page allows users to analyze logs:
   - by manually writing KQL queries,
   - or through **AI Mode**, which generates KQL suggestions from natural-language prompts.

---

## 3. Functional Modules

### 3.1 Asset Inventory

Pages:

- Capacities
- Workspaces
- Datasets
- Reports
- Lakehouses
- Warehouses

Features:

- Filtering and sorting
- Navigation to detailed views
- Lakehouse and Warehouse details retrieve fresh data from the Fabric REST API for the selected workspace and resolve the requested item by ID

### 3.2 SQL Cache

The primary data source for the UI.

Core entities:

- Capacity
- Workspace
- Dataset
- Report
- Lakehouse
- Warehouse

Each record can be marked as:

- Active
- Inactive

### 3.3 Event Log

Features include:

- **KQL Query** for manual log analysis
- **Aggregated Logs**
- **Anomaly Detection**
- **Table Summary**

#### AI Mode

1. The user enters a natural-language prompt.
2. The AI model generates a proposed KQL query.
3. The user clicks **Apply** to copy the generated query into the main query editor.

### 3.4 Roles and Access Control

An Entra ID administrative group named `PowerBiInventoryAdmin` is used to manage elevated access.

Administrators have:

- Extended visibility into application objects
- Additional access to administrative functionality

Regular users operate through a per-user permissions cache.

---

## 4. Technical Architecture

### Technology Stack

- Frontend / Server: Blazor Server (`blz0/blz0`)
- Database: SQL Server + Entity Framework Core

### Integrations

- Power BI REST API
- Fabric REST API
- Azure Data Explorer (Kusto)
- Microsoft Entra ID (authentication)
- LM Studio (AI-powered KQL generation)

### Static Assets

- Bootstrap is stored locally under `wwwroot/lib/bootstrap` as a fallback for Docker and development environments without reliable CDN access.
- Bootstrap Icons are downloaded locally via `libman.json` into `wwwroot/lib/bootstrap-icons`.

### Architecture Details

See:

- `architecture.md`

---

## 5. Configuration

### Key Configuration Sections

- `ConnectionStrings:blz0ContextSql`
- `ApiCredentials:*`
- `AzureAd:*`
- `Kusto:*`
- `Authorization:*`
- `LmStudio:*`

### Required Delegated Permissions

#### Power BI REST API

Permissions configured on the Power BI API and accessed through `.default`.

#### Fabric REST API

- `Workspace.Read.All`

#### Fabric Lakehouse Access

- `Lakehouse.Read.All`

#### Fabric Warehouse SQL Endpoint

Delegated access to:

`https://database.windows.net/.default`

### Development Example (LM Studio)

```text
LmStudio:BaseUrl = http://host.docker.internal:1234
LmStudio:Model = openai/gpt-oss-20b
```

---

## 6. Running the Application (Development)

1. Start SQL Server.
2. Configure `.env` and/or `appsettings.Development.json`.
3. Apply Entity Framework migrations.
4. Start the application.

Additional commands and setup instructions are available in:

- `README.md`

---

## 7. Maintenance and Troubleshooting

### Common Issues

#### 401 / 403 Errors from Power BI API

Usually caused by:

- Missing API permissions
- Missing admin consent
- Incorrect authentication configuration

#### `WorkspaceTypeNotSupported` for Lakehouses or Warehouses

The workspace does not support Fabric Item APIs.

Global refresh operations automatically skip unsupported workspaces.

#### HTTPS Certificate Issues in Containers

Verify:

- Launch profile configuration
- Host certificate settings
- Container networking configuration

#### AI Mode Is Not Responding

Verify that:

- LM Studio is running
- The configured endpoint is reachable from the application container

---

## 8. Future Enhancements

Potential future improvements include:

- Storing prompt-to-generated-KQL history
- KQL validation and normalization before execution
- Additional Event Log dashboards with predefined analytical scenarios
- Further optimization of per-user caching and REST API call reduction
- Optional migration of Lakehouse and Warehouse details from workspace-level lookups to dedicated **Get Item By ID** endpoints if broader Fabric Item API permissions become available