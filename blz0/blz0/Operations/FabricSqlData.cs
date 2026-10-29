using Microsoft.EntityFrameworkCore;
using blz0.Models;
using blz0.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace blz0.Operations
{

    public class FabricSqlData
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public List<Workspace>? workspacesList;
        public List<Dataset>? datasetsList;
        public List<Capacity>? capacitiesList;
        public List<Report>? reportsList;
        public List<Lakehouse>? lakehousesList;
        public List<Warehouse>? warehousesList;
        public int? capacitiesCount;
        public int? workspacesCount;
        public int? datasetsCount;
        public int? reportsCount;
        public int? lakehousesCount;
        public int? warehousesCount;
        public CacheRefreshState? currentCacheState;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FabricSqlData(
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub> hubContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _httpContextAccessor = httpContextAccessor;
        }

        private (string UserObjectId, string? UserName)? GetCurrentUserContext()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userObjectId = user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrWhiteSpace(userObjectId))
            {
                return null;
            }

            return (userObjectId.Trim().ToLowerInvariant(), user.Identity?.Name);
        }

        public async Task UpsertCurrentUserMapAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userContext = GetCurrentUserContext();
            if (userContext == null)
            {
                return;
            }

            var tenantId = user?.FindFirst("tid")?.Value?.Trim().ToLowerInvariant();
            var userKey = GetCurrentUserKey(user);
            var now = DateTime.UtcNow;

            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var userMap = await _db.UserObjectMap.SingleOrDefaultAsync(e => e.UserObjectId == userContext.Value.UserObjectId);

                if (userMap == null)
                {
                    userMap = new UserObjectMap
                    {
                        UserObjectId = userContext.Value.UserObjectId,
                        CreatedAtUtc = now
                    };
                    await _db.UserObjectMap.AddAsync(userMap);
                }

                userMap.UserKey = userKey;
                userMap.UserName = userContext.Value.UserName;
                userMap.TenantId = tenantId;
                userMap.LastSeenAtUtc = now;
                await _db.SaveChangesAsync();
            }
        }

        private string GetCurrentUserObjectIdOrEmpty()
        {
            return GetCurrentUserContext()?.UserObjectId ?? string.Empty;
        }

        private static string? GetCurrentUserKey(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userName = user.Identity?.Name;
            var objectId = user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            var userKey = !string.IsNullOrWhiteSpace(userName)
                ? userName.Trim().ToLowerInvariant()
                : objectId?.Trim().ToLowerInvariant();

            return string.IsNullOrWhiteSpace(userKey) ? null : userKey;
        }

        private static List<string>? NormalizeAllowedIds(IEnumerable<string>? allowedIds)
        {
            if (allowedIds == null)
            {
                return null;
            }

            return allowedIds
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        public async Task GetCapacitiesSqlAsync(string activeFilter = "active", IEnumerable<string>? allowedCapacityIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Capacity.AsQueryable();
                var allowedCapacityIdList = NormalizeAllowedIds(allowedCapacityIds);
                if (!string.IsNullOrWhiteSpace(currentUserObjectId))
                {
                    query = query.Where(e => e.UserObjectId == currentUserObjectId);
                }
                if (allowedCapacityIdList != null)
                {
                    query = allowedCapacityIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.Id != null && allowedCapacityIdList.Contains(e.Id));
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };
                this.capacitiesList = await query.ToListAsync();
            }
        }

        public async Task<List<string>?> GetValidUserAccessObjectIdsAsync(
            string userKey,
            string objectType,
            TimeSpan ttl,
            string? scopeId = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var query = _db.UserPowerBiAccessCache
                    .Where(e => e.UserKey == userKey && e.ObjectType == objectType);

                if (scopeId != null)
                {
                    query = query.Where(e => e.ScopeId == scopeId);
                }

                var entries = await query.ToListAsync();
                if (entries.Count == 0)
                {
                    return null;
                }

                var oldestEntryUtc = entries.Min(e => e.FetchedAtUtc);
                if (DateTime.UtcNow - oldestEntryUtc > ttl)
                {
                    return null;
                }

                return entries
                    .Select(e => e.ObjectId)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        public async Task ReplaceUserAccessCacheAsync(
            string userKey,
            string? userName,
            IEnumerable<string> capacityIds,
            IEnumerable<string> workspaceIds,
            string? workspaceScopeId = null)
        {
            var fetchedAtUtc = DateTime.UtcNow;
            var capacityItems = capacityIds
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(e => new UserPowerBiAccessCache
                {
                    UserKey = userKey,
                    UserName = userName,
                    ObjectType = "capacity",
                    ObjectId = e,
                    ScopeId = null,
                    FetchedAtUtc = fetchedAtUtc
                })
                .ToList();
            var workspaceItems = workspaceIds
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(e => new UserPowerBiAccessCache
                {
                    UserKey = userKey,
                    UserName = userName,
                    ObjectType = "workspace",
                    ObjectId = e,
                    ScopeId = workspaceScopeId,
                    FetchedAtUtc = fetchedAtUtc
                })
                .ToList();

            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var oldCapacities = await _db.UserPowerBiAccessCache
                    .Where(e => e.UserKey == userKey && e.ObjectType == "capacity")
                    .ToListAsync();
                var oldWorkspacesQuery = _db.UserPowerBiAccessCache
                    .Where(e => e.UserKey == userKey && e.ObjectType == "workspace");

                if (workspaceScopeId != null)
                {
                    oldWorkspacesQuery = oldWorkspacesQuery.Where(e => e.ScopeId == workspaceScopeId);
                }

                var oldWorkspaces = await oldWorkspacesQuery.ToListAsync();

                _db.UserPowerBiAccessCache.RemoveRange(oldCapacities);
                _db.UserPowerBiAccessCache.RemoveRange(oldWorkspaces);
                await _db.UserPowerBiAccessCache.AddRangeAsync(capacityItems);
                await _db.UserPowerBiAccessCache.AddRangeAsync(workspaceItems);
                await _db.SaveChangesAsync();
            }
        }

        public async Task GetWorkspacesSqlAsync(string? capacityIdNav, string activeFilter = "active", IEnumerable<string>? allowedWorkspaceIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Workspace.AsQueryable();
                var allowedWorkspaceIdList = NormalizeAllowedIds(allowedWorkspaceIds);
                if (!string.IsNullOrWhiteSpace(currentUserObjectId))
                {
                    query = query.Where(e => e.UserObjectId == currentUserObjectId);
                }
                if (allowedWorkspaceIdList != null)
                {
                    query = allowedWorkspaceIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.Id != null && allowedWorkspaceIdList.Contains(e.Id));
                }

                if (capacityIdNav != null)
                {
                    query = query.Where(e => e.CapacityId == capacityIdNav);
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };
                this.workspacesList = await query.ToListAsync();
            }
        }

        public async Task<List<string>> GetActiveWorkspaceIdsAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                return await _db.Workspace
                    .Where(e => e.UserObjectId == currentUserObjectId)
                    .Where(e => e.IsActive && e.Id != null)
                    .Select(e => e.Id!)
                    .ToListAsync();
            }
        }

        public async Task AddWorkspaceToSqlAsync(IEnumerable<Workspace> workspaceListApi, string? capacityIdNav = null, bool markMissingInactive = true)
        {
            var workspaceItems = workspaceListApi
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var rowCount = workspaceItems.Count;
            var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var activeIds = workspaceItems
                    .Select(e => e.Id)
                    .Where(e => e != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var existingScope = _db.Workspace.AsQueryable();
                existingScope = existingScope.Where(e => e.UserObjectId == currentUserObjectId);
                if (capacityIdNav != null)
                {
                    existingScope = existingScope.Where(e => e.CapacityId == capacityIdNav);
                }

                if (markMissingInactive)
                {
                    foreach (var workspaceObject in await existingScope.ToListAsync())
                    {
                        workspaceObject.IsActive = workspaceObject.Id != null && activeIds.Contains(workspaceObject.Id);
                    }
                }

                foreach (var workspaceObject in workspaceItems)
                {
                    var insertOrUpdate = await _db.Workspace.SingleOrDefaultAsync(e =>
                        e.UserObjectId == currentUserObjectId && e.Id == workspaceObject.Id);
                    workspaceObject.IsActive = true;
                    workspaceObject.UserObjectId = currentUserObjectId;

                    if (insertOrUpdate == null)
                    {
                        workspaceObject.InsertDate = DateTime.UtcNow;
                        workspaceObject.UpdateDate = null;
                        await _db.Workspace.AddAsync(workspaceObject);
                        Console.WriteLine("SQL INSERT");
                        Console.WriteLine(workspaceObject.InsertDate);
                    }
                    else
                    {
                        var originalInsertDate = insertOrUpdate.InsertDate;
                        _db.Entry(insertOrUpdate).CurrentValues.SetValues(workspaceObject);
                        insertOrUpdate.InsertDate = originalInsertDate;
                        insertOrUpdate.UpdateDate = DateTime.UtcNow;
                        insertOrUpdate.IsActive = true;
                        Console.WriteLine("SQL UPDATE");
                        
                        Console.WriteLine(workspaceObject.UpdateDate);
                        
                    }
                }
                await _db.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", "A new record was added!");
            }
            await MarkCacheRefreshedAsync("workspacesList", capacityIdNav, rowCount);
        }

        public async Task AddCapacityToSqlAsync(IEnumerable<Capacity> capacityListApi, bool markMissingInactive = true)
        {
            var capacityItems = capacityListApi
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var rowCount = capacityItems.Count;
            var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var activeIds = capacityItems
                    .Select(e => e.Id)
                    .Where(e => e != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (markMissingInactive)
                {
                    foreach (var capacityObject in await _db.Capacity.Where(e => e.UserObjectId == currentUserObjectId).ToListAsync())
                    {
                        capacityObject.IsActive = capacityObject.Id != null && activeIds.Contains(capacityObject.Id);
                    }
                }

                foreach (var capacityObject in capacityItems)
                {
                    var insertOrUpdate = await _db.Capacity.SingleOrDefaultAsync(e =>
                        e.UserObjectId == currentUserObjectId && e.Id == capacityObject.Id);
                    capacityObject.IsActive = true;
                    capacityObject.UserObjectId = currentUserObjectId;

                    if (insertOrUpdate == null)
                    {
                        capacityObject.InsertDate = DateTime.UtcNow;
                        capacityObject.UpdateDate = null;
                        await _db.Capacity.AddAsync(capacityObject);
                    }
                    else
                    {
                        var originalInsertDate = insertOrUpdate.InsertDate;
                        _db.Entry(insertOrUpdate).CurrentValues.SetValues(capacityObject);
                        insertOrUpdate.InsertDate = originalInsertDate;
                        insertOrUpdate.UpdateDate = DateTime.UtcNow;
                        insertOrUpdate.IsActive = true;
                    }
                }
                await _db.SaveChangesAsync();

            }
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "A new record was added!");
            await MarkCacheRefreshedAsync("capacitiesList", null, rowCount);
            Console.WriteLine("Added!!!!!");
        }

        public async Task GetDatasetsSqlAsync(
            string? workspaceIdNav,
            string activeFilter = "active",
            IEnumerable<string>? allowedDatasetIds = null,
            IEnumerable<string>? allowedWorkspaceIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Dataset.AsQueryable();
                var allowedDatasetIdList = NormalizeAllowedIds(allowedDatasetIds);
                var allowedWorkspaceIdList = NormalizeAllowedIds(allowedWorkspaceIds);
                if (!string.IsNullOrWhiteSpace(currentUserObjectId))
                {
                    query = query.Where(e => e.UserObjectId == currentUserObjectId);
                }
                if (allowedDatasetIdList != null)
                {
                    query = allowedDatasetIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.Id != null && allowedDatasetIdList.Contains(e.Id));
                }
                if (allowedWorkspaceIdList != null)
                {
                    query = allowedWorkspaceIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.WorkspaceId != null && allowedWorkspaceIdList.Contains(e.WorkspaceId));
                }
                if (workspaceIdNav != null)
                {
                    query = query.Where(e => e.WorkspaceId == workspaceIdNav);
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };
                this.datasetsList = await query.ToListAsync();
            }
        }
        public async Task AddDatasetToSqlAsync(IEnumerable<Dataset> datasetListApi, string? workspaceIdNav, bool notify = true)
        {
            if (workspaceIdNav == null)
            {
                throw new ArgumentException("Workspace id is required when refreshing datasets.", nameof(workspaceIdNav));
            }

            var datasetItems = datasetListApi
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var rowCount = datasetItems.Count;
            var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var activeIds = datasetItems
                    .Select(e => e.Id)
                    .Where(e => e != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var datasetObject in await _db.Dataset
                    .Where(e => e.UserObjectId == currentUserObjectId && e.WorkspaceId == workspaceIdNav)
                    .ToListAsync())
                {
                    datasetObject.IsActive = datasetObject.Id != null && activeIds.Contains(datasetObject.Id);
                }

                foreach (var datasetObject in datasetItems)
                {
                    var insertOrUpdate = await _db.Dataset.SingleOrDefaultAsync(e =>
                        e.UserObjectId == currentUserObjectId && e.Id == datasetObject.Id);
                    datasetObject.IsActive = true;
                    datasetObject.UserObjectId = currentUserObjectId;

                    if (insertOrUpdate == null)
                    {
                        datasetObject.InsertDate = DateTime.UtcNow;
                        datasetObject.UpdateDate = null;
                        datasetObject.WorkspaceId = workspaceIdNav;
                        await _db.Dataset.AddAsync(datasetObject);
                    }
                    else
                    {
                        var originalInsertDate = insertOrUpdate.InsertDate;
                        var originalLastRefreshAtUtc = insertOrUpdate.LastRefreshAtUtc;
                        var originalLastRefreshStatus = insertOrUpdate.LastRefreshStatus;
                        var originalLastRefreshCacheUpdatedAtUtc = insertOrUpdate.LastRefreshCacheUpdatedAtUtc;
                        _db.Entry(insertOrUpdate).CurrentValues.SetValues(datasetObject);
                        insertOrUpdate.InsertDate = originalInsertDate;
                        insertOrUpdate.UpdateDate = DateTime.UtcNow;
                        insertOrUpdate.WorkspaceId = workspaceIdNav;
                        insertOrUpdate.LastRefreshAtUtc = originalLastRefreshAtUtc;
                        insertOrUpdate.LastRefreshStatus = originalLastRefreshStatus;
                        insertOrUpdate.LastRefreshCacheUpdatedAtUtc = originalLastRefreshCacheUpdatedAtUtc;
                        insertOrUpdate.IsActive = true;
                    }
                }
                await _db.SaveChangesAsync();
                if (notify)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Datasets cache refreshed.");
                }
            }
            await MarkCacheRefreshedAsync("datasetsList", workspaceIdNav, rowCount);
        }


        public async Task GetReportsSqlAsync(
            string? workspaceIdNav,
            string activeFilter = "active",
            IEnumerable<string>? allowedReportIds = null,
            IEnumerable<string>? allowedWorkspaceIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Report.AsQueryable();
                var allowedReportIdList = NormalizeAllowedIds(allowedReportIds);
                var allowedWorkspaceIdList = NormalizeAllowedIds(allowedWorkspaceIds);
                if (!string.IsNullOrWhiteSpace(currentUserObjectId))
                {
                    query = query.Where(e => e.UserObjectId == currentUserObjectId);
                }
                if (allowedReportIdList != null)
                {
                    query = allowedReportIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.Id != null && allowedReportIdList.Contains(e.Id));
                }
                if (allowedWorkspaceIdList != null)
                {
                    query = allowedWorkspaceIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.WorkspaceId != null && allowedWorkspaceIdList.Contains(e.WorkspaceId));
                }
                if (workspaceIdNav != null)
                {
                    query = query.Where(e => e.WorkspaceId == workspaceIdNav);
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };
                this.reportsList = await query.ToListAsync();
            }
        }
        public async Task AddReportToSqlAsync(IEnumerable<Report> reportListApi, string? workspaceIdNav, bool notify = true)
        {
            if (workspaceIdNav == null)
            {
                throw new ArgumentException("Workspace id is required when refreshing reports.", nameof(workspaceIdNav));
            }

            var reportItems = reportListApi
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var rowCount = reportItems.Count;
            var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var activeIds = reportItems
                    .Select(e => e.Id)
                    .Where(e => e != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var reportObject in await _db.Report
                    .Where(e => e.UserObjectId == currentUserObjectId && e.WorkspaceId == workspaceIdNav)
                    .ToListAsync())
                {
                    reportObject.IsActive = reportObject.Id != null && activeIds.Contains(reportObject.Id);
                }

                foreach (var reportObject in reportItems)
                {
                    var insertOrUpdate = _db.Report.Local.FirstOrDefault(e => e.Id == reportObject.Id)
                        ?? await _db.Report.SingleOrDefaultAsync(e =>
                            e.UserObjectId == currentUserObjectId && e.Id == reportObject.Id);
                    reportObject.IsActive = true;
                    reportObject.UserObjectId = currentUserObjectId;

                    if (insertOrUpdate == null)
                    {
                        reportObject.InsertDate = DateTime.UtcNow;
                        reportObject.UpdateDate = null;
                        reportObject.WorkspaceId = workspaceIdNav;
                        await _db.Report.AddAsync(reportObject);
                    }
                    else
                    {
                        var originalInsertDate = insertOrUpdate.InsertDate;
                        _db.Entry(insertOrUpdate).CurrentValues.SetValues(reportObject);
                        insertOrUpdate.InsertDate = originalInsertDate;
                        insertOrUpdate.UpdateDate = DateTime.UtcNow;
                        insertOrUpdate.WorkspaceId = workspaceIdNav;
                        insertOrUpdate.IsActive = true;
                    }
                }
                await _db.SaveChangesAsync();
                if (notify)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Reports cache refreshed.");
                }
            }
            await MarkCacheRefreshedAsync("reportsList", workspaceIdNav, rowCount);
        }

        public async Task GetLakehousesSqlAsync(
            string? workspaceIdNav,
            string activeFilter = "active",
            IEnumerable<string>? allowedWorkspaceIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Lakehouse.AsQueryable();
                var allowedWorkspaceIdList = NormalizeAllowedIds(allowedWorkspaceIds);
                if (!string.IsNullOrWhiteSpace(currentUserObjectId))
                {
                    query = query.Where(e => e.UserObjectId == currentUserObjectId);
                }
                if (allowedWorkspaceIdList != null)
                {
                    query = allowedWorkspaceIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.WorkspaceId != null && allowedWorkspaceIdList.Contains(e.WorkspaceId));
                }
                if (workspaceIdNav != null)
                {
                    query = query.Where(e => e.WorkspaceId == workspaceIdNav);
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };
                lakehousesList = await query.ToListAsync();
            }
        }

        public async Task AddLakehouseToSqlAsync(IEnumerable<Lakehouse> lakehouseListApi, string? workspaceIdNav, bool notify = true)
        {
            if (workspaceIdNav == null)
            {
                throw new ArgumentException("Workspace id is required when refreshing lakehouses.", nameof(workspaceIdNav));
            }

            var lakehouseItems = lakehouseListApi
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var rowCount = lakehouseItems.Count;
            var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var activeIds = lakehouseItems
                    .Select(e => e.Id)
                    .Where(e => e != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var lakehouseObject in await _db.Lakehouse
                    .Where(e => e.UserObjectId == currentUserObjectId && e.WorkspaceId == workspaceIdNav)
                    .ToListAsync())
                {
                    lakehouseObject.IsActive = lakehouseObject.Id != null && activeIds.Contains(lakehouseObject.Id);
                }

                foreach (var lakehouseObject in lakehouseItems)
                {
                    var insertOrUpdate = await _db.Lakehouse.SingleOrDefaultAsync(e =>
                        e.UserObjectId == currentUserObjectId && e.Id == lakehouseObject.Id);
                    lakehouseObject.IsActive = true;
                    lakehouseObject.UserObjectId = currentUserObjectId;
                    lakehouseObject.WorkspaceId = string.IsNullOrWhiteSpace(lakehouseObject.WorkspaceId)
                        ? workspaceIdNav
                        : lakehouseObject.WorkspaceId;

                    if (insertOrUpdate == null)
                    {
                        lakehouseObject.InsertDate = DateTime.UtcNow;
                        lakehouseObject.UpdateDate = null;
                        await _db.Lakehouse.AddAsync(lakehouseObject);
                    }
                    else
                    {
                        var originalInsertDate = insertOrUpdate.InsertDate;
                        _db.Entry(insertOrUpdate).CurrentValues.SetValues(lakehouseObject);
                        insertOrUpdate.InsertDate = originalInsertDate;
                        insertOrUpdate.UpdateDate = DateTime.UtcNow;
                        insertOrUpdate.WorkspaceId = lakehouseObject.WorkspaceId;
                        insertOrUpdate.IsActive = true;
                    }
                }
                await _db.SaveChangesAsync();
                if (notify)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Lakehouses cache refreshed.");
                }
            }
            await MarkCacheRefreshedAsync("lakehousesList", workspaceIdNav, rowCount);
        }

        public async Task GetWarehousesSqlAsync(
            string? workspaceIdNav,
            string activeFilter = "active",
            IEnumerable<string>? allowedWorkspaceIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Warehouse.AsQueryable();
                var allowedWorkspaceIdList = NormalizeAllowedIds(allowedWorkspaceIds);
                if (!string.IsNullOrWhiteSpace(currentUserObjectId))
                {
                    query = query.Where(e => e.UserObjectId == currentUserObjectId);
                }
                if (allowedWorkspaceIdList != null)
                {
                    query = allowedWorkspaceIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.WorkspaceId != null && allowedWorkspaceIdList.Contains(e.WorkspaceId));
                }
                if (workspaceIdNav != null)
                {
                    query = query.Where(e => e.WorkspaceId == workspaceIdNav);
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };
                warehousesList = await query.ToListAsync();
            }
        }

        public async Task AddWarehouseToSqlAsync(IEnumerable<Warehouse> warehouseListApi, string? workspaceIdNav, bool notify = true)
        {
            if (workspaceIdNav == null)
            {
                throw new ArgumentException("Workspace id is required when refreshing warehouses.", nameof(workspaceIdNav));
            }

            var warehouseItems = warehouseListApi
                .Where(e => !string.IsNullOrWhiteSpace(e.Id))
                .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            var rowCount = warehouseItems.Count;
            var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var activeIds = warehouseItems
                    .Select(e => e.Id)
                    .Where(e => e != null)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var warehouseObject in await _db.Warehouse
                    .Where(e => e.UserObjectId == currentUserObjectId && e.WorkspaceId == workspaceIdNav)
                    .ToListAsync())
                {
                    warehouseObject.IsActive = warehouseObject.Id != null && activeIds.Contains(warehouseObject.Id);
                }

                foreach (var warehouseObject in warehouseItems)
                {
                    var insertOrUpdate = await _db.Warehouse.SingleOrDefaultAsync(e =>
                        e.UserObjectId == currentUserObjectId && e.Id == warehouseObject.Id);
                    warehouseObject.IsActive = true;
                    warehouseObject.UserObjectId = currentUserObjectId;
                    warehouseObject.WorkspaceId = string.IsNullOrWhiteSpace(warehouseObject.WorkspaceId)
                        ? workspaceIdNav
                        : warehouseObject.WorkspaceId;

                    if (insertOrUpdate == null)
                    {
                        warehouseObject.InsertDate = DateTime.UtcNow;
                        warehouseObject.UpdateDate = null;
                        await _db.Warehouse.AddAsync(warehouseObject);
                    }
                    else
                    {
                        var originalInsertDate = insertOrUpdate.InsertDate;
                        _db.Entry(insertOrUpdate).CurrentValues.SetValues(warehouseObject);
                        insertOrUpdate.InsertDate = originalInsertDate;
                        insertOrUpdate.UpdateDate = DateTime.UtcNow;
                        insertOrUpdate.WorkspaceId = warehouseObject.WorkspaceId;
                        insertOrUpdate.IsActive = true;
                    }
                }
                await _db.SaveChangesAsync();
                if (notify)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Warehouses cache refreshed.");
                }
            }
            await MarkCacheRefreshedAsync("warehousesList", workspaceIdNav, rowCount);
        }

#pragma warning disable CS1998
        public async Task GetChartsCountAsync()
        {
            Console.WriteLine(1);
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                capacitiesCount = _db.Capacity.Count(e => e.UserObjectId == currentUserObjectId && e.IsActive);
                workspacesCount = _db.Workspace.Count(e => e.UserObjectId == currentUserObjectId && e.IsActive);
                datasetsCount = _db.Dataset.Count(e => e.UserObjectId == currentUserObjectId && e.IsActive);
                reportsCount = _db.Report.Count(e => e.UserObjectId == currentUserObjectId && e.IsActive);
            }
        }
#pragma warning restore CS1998

        public async Task GetCacheStateAsync(string objectType, string? scopeId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var cacheKey = GetCacheKey(objectType, scopeId, currentUserObjectId);
                currentCacheState = await FindCacheStateAsync(_db, cacheKey, currentUserObjectId, objectType, scopeId);
            }
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var capacities = await _db.Capacity.Where(e => e.UserObjectId == currentUserObjectId).ToListAsync();
                var workspaces = await _db.Workspace.Where(e => e.UserObjectId == currentUserObjectId).ToListAsync();
                var datasets = await _db.Dataset.Where(e => e.UserObjectId == currentUserObjectId).ToListAsync();
                var reports = await _db.Report.Where(e => e.UserObjectId == currentUserObjectId).ToListAsync();

                var workspaceNames = workspaces
                    .Where(e => e.Id != null)
                    .ToDictionary(e => e.Id!, e => string.IsNullOrWhiteSpace(e.Name) ? e.Id! : e.Name!);

                var capacityNames = capacities
                    .Where(e => e.Id != null)
                    .ToDictionary(e => e.Id!, e => string.IsNullOrWhiteSpace(e.DisplayName) ? e.Id! : e.DisplayName!);

                return new DashboardStats
                {
                    ActiveCapacities = capacities.Count(e => e.IsActive),
                    ActiveWorkspaces = workspaces.Count(e => e.IsActive),
                    ActiveDatasets = datasets.Count(e => e.IsActive),
                    ActiveReports = reports.Count(e => e.IsActive),
                    InactiveCapacities = capacities.Count(e => !e.IsActive),
                    InactiveWorkspaces = workspaces.Count(e => !e.IsActive),
                    InactiveDatasets = datasets.Count(e => !e.IsActive),
                    InactiveReports = reports.Count(e => !e.IsActive),
                    WorkspacesByCapacity = workspaces
                        .Where(e => e.IsActive)
                        .GroupBy(e => e.CapacityId ?? "No capacity")
                        .Select(e => new DashboardSlice { Label = GetLabel(capacityNames, e.Key), Value = e.Count() })
                        .OrderByDescending(e => e.Value)
                        .ThenBy(e => e.Label)
                        .Take(8)
                        .ToList(),
                    DatasetsByWorkspace = datasets
                        .Where(e => e.IsActive)
                        .GroupBy(e => e.WorkspaceId ?? "No workspace")
                        .Select(e => new DashboardSlice { Label = GetLabel(workspaceNames, e.Key), Value = e.Count() })
                        .OrderByDescending(e => e.Value)
                        .ThenBy(e => e.Label)
                        .Take(8)
                        .ToList(),
                    ReportsByWorkspace = reports
                        .Where(e => e.IsActive)
                        .GroupBy(e => e.WorkspaceId ?? "No workspace")
                        .Select(e => new DashboardSlice { Label = GetLabel(workspaceNames, e.Key), Value = e.Count() })
                        .OrderByDescending(e => e.Value)
                        .ThenBy(e => e.Label)
                        .Take(8)
                        .ToList(),
                    DatasetStorageModes = datasets
                        .Where(e => e.IsActive)
                        .GroupBy(e => string.IsNullOrWhiteSpace(e.TargetStorageMode) ? "Unknown" : e.TargetStorageMode!)
                        .Select(e => new DashboardSlice { Label = e.Key, Value = e.Count() })
                        .OrderByDescending(e => e.Value)
                        .ThenBy(e => e.Label)
                        .ToList(),
                    ReportTypes = reports
                        .Where(e => e.IsActive)
                        .GroupBy(e => string.IsNullOrWhiteSpace(e.ReportType) ? "Unknown" : e.ReportType!)
                        .Select(e => new DashboardSlice { Label = e.Key, Value = e.Count() })
                        .OrderByDescending(e => e.Value)
                        .ThenBy(e => e.Label)
                        .ToList()
                };
            }
        }

        public async Task SetCacheRefreshedAsync(string objectType, string? scopeId, int rowCount)
        {
            await MarkCacheRefreshedAsync(objectType, scopeId, rowCount);
        }

        public async Task<List<Dataset>> GetDatasetsForRefreshCacheAsync(
            string? workspaceIdNav,
            string activeFilter = "active",
            IEnumerable<string>? allowedDatasetIds = null,
            IEnumerable<string>? allowedWorkspaceIds = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var query = _db.Dataset.AsQueryable().Where(e => e.UserObjectId == currentUserObjectId);
                var allowedDatasetIdList = NormalizeAllowedIds(allowedDatasetIds);
                var allowedWorkspaceIdList = NormalizeAllowedIds(allowedWorkspaceIds);
                if (allowedDatasetIdList != null)
                {
                    query = allowedDatasetIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.Id != null && allowedDatasetIdList.Contains(e.Id));
                }
                if (allowedWorkspaceIdList != null)
                {
                    query = allowedWorkspaceIdList.Count == 0
                        ? query.Where(e => false)
                        : query.Where(e => e.WorkspaceId != null && allowedWorkspaceIdList.Contains(e.WorkspaceId));
                }
                if (workspaceIdNav != null)
                {
                    query = query.Where(e => e.WorkspaceId == workspaceIdNav);
                }

                query = activeFilter switch
                {
                    "inactive" => query.Where(e => !e.IsActive),
                    "all" => query,
                    _ => query.Where(e => e.IsActive)
                };

                return await query.ToListAsync();
            }
        }

        public async Task SendRefreshNotificationAsync(string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
        }

        public async Task UpdateDatasetRefreshCacheAsync(
            string datasetId,
            string? workspaceId,
            DateTime? lastRefreshAtUtc,
            string? lastRefreshStatus,
            string? userObjectId = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = string.IsNullOrWhiteSpace(userObjectId)
                    ? GetCurrentUserObjectIdOrEmpty()
                    : userObjectId.Trim().ToLowerInvariant();
                var dataset = await _db.Dataset.SingleOrDefaultAsync(e => e.UserObjectId == currentUserObjectId && e.Id == datasetId && e.WorkspaceId == workspaceId);
                if (dataset == null)
                {
                    return;
                }

                dataset.LastRefreshAtUtc = lastRefreshAtUtc;
                dataset.LastRefreshStatus = lastRefreshStatus;
                dataset.LastRefreshCacheUpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        private static string GetLabel(Dictionary<string, string> labels, string key)
        {
            return labels.TryGetValue(key, out var label) ? label : key;
        }

        private async Task MarkCacheRefreshedAsync(string objectType, string? scopeId, int rowCount)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _db = scope.ServiceProvider.GetRequiredService<blz0Context>();
                var currentUserObjectId = GetCurrentUserObjectIdOrEmpty();
                var cacheKey = GetCacheKey(objectType, scopeId, currentUserObjectId);
                var cacheState = await FindCacheStateAsync(_db, cacheKey, currentUserObjectId, objectType, scopeId);

                if (cacheState == null)
                {
                    cacheState = new CacheRefreshState
                    {
                        ObjectType = objectType,
                        ScopeId = scopeId,
                        UserObjectId = currentUserObjectId,
                    };
                    await _db.CacheRefreshState.AddAsync(cacheState);
                }

                cacheState.CacheKey = cacheKey;
                cacheState.ObjectType = objectType;
                cacheState.ScopeId = scopeId;
                cacheState.UserObjectId = currentUserObjectId;
                cacheState.RefreshedAtUtc = DateTime.UtcNow;
                cacheState.RowCount = rowCount;
                await _db.SaveChangesAsync();
                currentCacheState = cacheState;
            }
        }

        private static async Task<CacheRefreshState?> FindCacheStateAsync(
            blz0Context db,
            string cacheKey,
            string userObjectId,
            string objectType,
            string? scopeId)
        {
            var cacheState = await db.CacheRefreshState.SingleOrDefaultAsync(e => e.CacheKey == cacheKey);
            if (cacheState != null)
            {
                return cacheState;
            }

            var query = db.CacheRefreshState
                .Where(e => e.UserObjectId == userObjectId && e.ObjectType == objectType);

            query = scopeId == null
                ? query.Where(e => e.ScopeId == null)
                : query.Where(e => e.ScopeId == scopeId);

            return await query.SingleOrDefaultAsync();
        }

        private static string GetCacheKey(string objectType, string? scopeId, string userObjectId)
        {
            return $"{userObjectId}:{objectType}:{scopeId ?? "all"}";
        }
    }

}
