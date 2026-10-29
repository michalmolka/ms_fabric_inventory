using blz0.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace blz0.Operations
{
    public class WarehouseMetadataData(ITokenAcquisition tokenAcquisition)
    {
        public async Task<List<WarehouseTable>> GetWarehouseTablesAsync(
            ClaimsPrincipal user,
            string? connectionString,
            string? warehouseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Warehouse SQL connection string is missing.");
            }

            if (string.IsNullOrWhiteSpace(warehouseName))
            {
                throw new InvalidOperationException("Warehouse name is required as SQL Initial Catalog.");
            }

            var tenantId = user.FindFirst("tid")?.Value;
            var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
                new[] { "https://database.windows.net/.default" },
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
                tenantId: tenantId,
                user: user);

            await using var connection = new SqlConnection(BuildConnectionString(connectionString, warehouseName))
            {
                AccessToken = accessToken
            };

            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    TABLE_SCHEMA AS SchemaName,
                    TABLE_NAME AS TableName,
                    TABLE_TYPE AS TableType
                FROM INFORMATION_SCHEMA.TABLES
                ORDER BY TABLE_SCHEMA, TABLE_NAME;
                """;

            var tables = new List<WarehouseTable>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(new WarehouseTable
                {
                    SchemaName = reader["SchemaName"]?.ToString(),
                    TableName = reader["TableName"]?.ToString(),
                    TableType = NormalizeTableType(reader["TableType"]?.ToString())
                });
            }

            return tables;
        }

        private static string NormalizeTableType(string? tableType)
        {
            return tableType switch
            {
                "BASE TABLE" => "Table",
                "VIEW" => "View",
                _ => string.IsNullOrWhiteSpace(tableType) ? "-" : tableType
            };
        }

        private static string BuildConnectionString(string connectionString, string warehouseName)
        {
            var builder = new SqlConnectionStringBuilder();
            if (connectionString.Contains('='))
            {
                builder.ConnectionString = connectionString;
            }
            else
            {
                builder.DataSource = NormalizeDataSource(connectionString);
            }

            builder.InitialCatalog = warehouseName;
            builder.Encrypt = true;
            builder.TrustServerCertificate = false;
            builder.PersistSecurityInfo = false;

            if (builder.ConnectTimeout == 0)
            {
                builder.ConnectTimeout = 30;
            }

            builder.Remove("User ID");
            builder.Remove("Password");
            builder.Remove("Authentication");
            builder.Remove("MultipleActiveResultSets");

            return builder.ConnectionString;
        }

        private static string NormalizeDataSource(string dataSource)
        {
            var result = dataSource.Trim();
            if (result.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                result = result["https://".Length..];
            }

            return result.TrimEnd('/');
        }
    }
}
