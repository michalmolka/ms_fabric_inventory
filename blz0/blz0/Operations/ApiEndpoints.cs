using blz0.Operations;
using blz0.Models;
using blz0.Authorization;
using System.Security.Claims;

public static class ApiEndpoints
{
    record EventLogsParam(int TakeValue, string AgoValue);
    record AiKqlRequest(string Prompt);

    public static void MapApiEndpoints(this WebApplication app)
    {
        // Add Minimal API Endpoints here
        app.MapGet("/api/ping", () => new { Message = "OK" });

        app.MapGet("/api/ping/{parameter01}", (string parameter01) =>
            new { Greeting = $"OK, {parameter01}!" });

        app.MapGet("/api/capacities/", async (HttpContext httpContext, IServiceProvider serviceProvider, IConfiguration configuration) =>
        {
            try
            {
                var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
                var access = await BuildAccessContextAsync(httpContext.User, serviceProvider, configuration, null);
                List<Capacity>? cls = null;
                await fabricSqlData.GetCapacitiesSqlAsync("active", access.IsAdmin ? null : access.AllowedCapacityIds);
                cls = fabricSqlData.capacitiesList;
                return Results.Ok(new { Status = "Success", ReceivedData = cls });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "GET /api/capacities", ex);
            }
        });




        app.MapGet("/api/workspaces/{capacityId?}", async (string? capacityId, HttpContext httpContext, IServiceProvider serviceProvider, IConfiguration configuration) =>
        {
            try
            {
                var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
                var access = await BuildAccessContextAsync(httpContext.User, serviceProvider, configuration, capacityId);
                List<Workspace>? wls = null;
                await fabricSqlData.GetWorkspacesSqlAsync(capacityId, "active", access.IsAdmin ? null : access.AllowedWorkspaceIds);
                wls = fabricSqlData.workspacesList;
                return Results.Ok(new { Status = "Success", ReceivedData = wls });
            }
            catch(Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "GET /api/workspaces", ex);
            }
        });

        app.MapGet("/api/datasets/{workspaceId?}", async (string? workspaceId, HttpContext httpContext, IServiceProvider serviceProvider, IConfiguration configuration) =>
        {
            try
            {
                var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
                var access = await BuildAccessContextAsync(httpContext.User, serviceProvider, configuration, null);
                List<Dataset>? dls = null;

                await fabricSqlData.GetDatasetsSqlAsync(workspaceId, "active", null, access.IsAdmin ? null : access.AllowedWorkspaceIds);
                dls = fabricSqlData.datasetsList;
                return Results.Ok(new { Status = "Success", ReceivedData = dls });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "GET /api/datasets", ex);
            }
        });

        app.MapGet("/api/reports/{workspaceId?}", async (string? workspaceId, HttpContext httpContext, IServiceProvider serviceProvider, IConfiguration configuration) =>
        {
            try
            {
                var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
                var access = await BuildAccessContextAsync(httpContext.User, serviceProvider, configuration, null);
                List<Report>? rls = null;

                await fabricSqlData.GetReportsSqlAsync(workspaceId, "active", null, access.IsAdmin ? null : access.AllowedWorkspaceIds);
                rls = fabricSqlData.reportsList;
                return Results.Ok(new { Status = "Success", ReceivedData = rls });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "GET /api/reports", ex);
            }
        });

        app.MapGet("/api/lakehouses/{workspaceId?}", async (string? workspaceId, HttpContext httpContext, IServiceProvider serviceProvider, IConfiguration configuration) =>
        {
            try
            {
                var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
                var access = await BuildAccessContextAsync(httpContext.User, serviceProvider, configuration, null);
                List<Lakehouse>? lakehouses = null;

                await fabricSqlData.GetLakehousesSqlAsync(workspaceId, "active", access.IsAdmin ? null : access.AllowedWorkspaceIds);
                lakehouses = fabricSqlData.lakehousesList;
                return Results.Ok(new { Status = "Success", ReceivedData = lakehouses });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "GET /api/lakehouses", ex);
            }
        });

        app.MapGet("/api/warehouses/{workspaceId?}", async (string? workspaceId, HttpContext httpContext, IServiceProvider serviceProvider, IConfiguration configuration) =>
        {
            try
            {
                var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
                var access = await BuildAccessContextAsync(httpContext.User, serviceProvider, configuration, null);
                List<Warehouse>? warehouses = null;

                await fabricSqlData.GetWarehousesSqlAsync(workspaceId, "active", access.IsAdmin ? null : access.AllowedWorkspaceIds);
                warehouses = fabricSqlData.warehousesList;
                return Results.Ok(new { Status = "Success", ReceivedData = warehouses });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "GET /api/warehouses", ex);
            }
        });


        app.MapPost("/api/eventlogs", async (EventLogsParam eventLogsParam, HttpContext httpContext, IServiceProvider serviceProvider) =>
        {
            try
            {
                var fabricKqlData = serviceProvider.GetRequiredService<FabricKqlData>();
                List<EventLogs>? els = null;

                await fabricKqlData.GetEventLogs(true, "", eventLogsParam.TakeValue, eventLogsParam.AgoValue);
                if (fabricKqlData.queryResult != null)
                {
                    els = fabricKqlData.queryResult.ToList();
                }
                return Results.Ok(new { Status = "Success", ReceivedData = els });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "POST /api/eventlogs", ex);
            }
        }
        );

        app.MapPost("/api/eventlogs/ai-kql", async (AiKqlRequest request, HttpContext httpContext, IServiceProvider serviceProvider) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return Results.BadRequest(new { Status = "Error", Message = "Prompt is required." });
                }

                var aiKqlService = serviceProvider.GetRequiredService<AiKqlService>();
                var generated = await aiKqlService.GenerateKqlAsync(request.Prompt);
                var validated = EnsureSafeKql(generated);
                return Results.Ok(new { Status = "Success", Query = validated });
            }
            catch (Exception ex)
            {
                return await LoggedProblemAsync(serviceProvider, httpContext, "POST /api/eventlogs/ai-kql", ex, "Failed to generate KQL query.");
            }
        });
    }

    private static async Task<IResult> LoggedProblemAsync(
        IServiceProvider serviceProvider,
        HttpContext httpContext,
        string operation,
        Exception exception,
        string title = "An error occurred while processing your request.")
    {
        await LogExceptionToFileAsync(serviceProvider, httpContext, operation, exception);
        return Results.Problem(
            statusCode: 500,
            title: title,
            detail: "Internal server error. Details were written to the server log.");
    }

    private static async Task LogExceptionToFileAsync(
        IServiceProvider serviceProvider,
        HttpContext httpContext,
        string operation,
        Exception exception)
    {
        try
        {
            var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var logDirectory = Path.Combine(environment.ContentRootPath, "Logs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "api-errors.log");
            var userName = httpContext.User?.Identity?.Name ?? "anonymous";
            var requestId = httpContext.TraceIdentifier;
            var message = $"""
                [{DateTimeOffset.UtcNow:O}] {operation}
                User: {userName}
                TraceId: {requestId}
                {exception}

                """;

            await File.AppendAllTextAsync(logPath, message);
        }
        catch
        {
            // Logging should not turn a handled API failure into another exception.
        }
    }

    private static string EnsureSafeKql(string query)
    {
        var normalized = query.Replace("\r", string.Empty).Trim().Trim('`');
        if (!normalized.StartsWith("['ms-fabric-logs']", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Generated query must start from ['ms-fabric-logs'].");
        }

        var blocked = new[] { ".create", ".drop", ".alter", ".delete", ".ingest", ".set", "externaldata(" };
        if (blocked.Any(token => normalized.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Generated query contains blocked operators.");
        }

        return normalized;
    }

    private sealed record AccessContext(
        bool IsAdmin,
        List<string>? AllowedCapacityIds,
        List<string>? AllowedWorkspaceIds);

    private static async Task<AccessContext> BuildAccessContextAsync(
        ClaimsPrincipal user,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        string? capacityId)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return new AccessContext(false, [], []);
        }

        var fabricAuth = serviceProvider.GetRequiredService<FabricAuth>();
        var fabricApiData = serviceProvider.GetRequiredService<FabricApiData>();
        var fabricSqlData = serviceProvider.GetRequiredService<FabricSqlData>();
        await fabricSqlData.UpsertCurrentUserMapAsync();

        if (await AdminAccessEvaluator.IsPowerBiInventoryAdminAsync(user, configuration))
        {
            return new AccessContext(true, null, null);
        }

        var userKey = GetUserKey(user);
        if (userKey != null)
        {
            var ttl = GetUserAccessCacheTtl(configuration);
            var cachedCapacityIds = await fabricSqlData.GetValidUserAccessObjectIdsAsync(userKey.Value.UserKey, "capacity", ttl);
            var cachedWorkspaceIds = await fabricSqlData.GetValidUserAccessObjectIdsAsync(userKey.Value.UserKey, "workspace", ttl, capacityId);

            if (cachedCapacityIds != null && cachedWorkspaceIds != null)
            {
                return new AccessContext(false, cachedCapacityIds, cachedWorkspaceIds);
            }
        }

        await fabricAuth.FabricAuthAutoAsync(user);
        var client = fabricAuth.tokenAuto;
        if (client == null)
        {
            return new AccessContext(false, [], []);
        }

        await fabricApiData.GetCapacitiesApiAsync(client);
        var allowedCapacityIds = (fabricApiData.capacitiesList ?? [])
            .Select(e => e.Id)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await fabricApiData.GetWorkspacesApiAsync(client, capacityId);
        var allowedWorkspaceIds = (fabricApiData.workspacesList ?? [])
            .Select(e => e.Id)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (userKey != null)
        {
            await fabricSqlData.ReplaceUserAccessCacheAsync(
                userKey.Value.UserKey,
                userKey.Value.UserName,
                allowedCapacityIds,
                allowedWorkspaceIds,
                capacityId);
        }

        return new AccessContext(false, allowedCapacityIds, allowedWorkspaceIds);
    }

    private static TimeSpan GetUserAccessCacheTtl(IConfiguration configuration)
    {
        return TimeSpan.FromMinutes(
            int.TryParse(configuration["Authorization:UserAccessCacheTtlMinutes"], out var minutes) && minutes > 0
                ? minutes
                : 60);
    }

    private static (string UserKey, string? UserName)? GetUserKey(ClaimsPrincipal user)
    {
        var userName = user.Identity?.Name;
        var objectId = user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        var userKey = !string.IsNullOrWhiteSpace(userName)
            ? userName.Trim().ToLowerInvariant()
            : objectId?.Trim().ToLowerInvariant();

        return string.IsNullOrWhiteSpace(userKey)
            ? null
            : (userKey, userName);
    }
}
