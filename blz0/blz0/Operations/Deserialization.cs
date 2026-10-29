using blz0.Components.Pages;
using blz0.Models;
using System.Text.Json;

namespace blz0.Operations
{
    public class Deserialization
    {
        public IEnumerable<Workspace> DeserializeWorkspace(string jsonString)
        {
            var workspaces01 = JsonSerializer.Deserialize<WorkspacesRegister>(jsonString);
            
            if (workspaces01?.Workspace == null)
            {
                return [];
            }

            var workspaces02 = workspaces01.Workspace.Where(d => true);
            return workspaces02;
        }
        public IEnumerable<Dataset> DeserializeDataset(string jsonString)
        {
            var datasets01 = JsonSerializer.Deserialize<DatasetsRegister>(jsonString);

            if (datasets01?.Dataset == null)
            {
                return [];
            }

            var resultDatasets01 = datasets01.Dataset.Where(d => true);
            return resultDatasets01;
        }
        public IEnumerable<Capacity> DeserializeCapacity(string jsonString)
        {
            var capacities01 = JsonSerializer.Deserialize<CapacitiesRegister>(jsonString);

            if (capacities01?.Capacity == null)
            {
                return [];
            }

            var resultcapacities01 = capacities01.Capacity.Where(d => true);
            return resultcapacities01;
        }
        public IEnumerable<Report> DeserializeReport(string jsonString)
        {
            var creports01 = JsonSerializer.Deserialize<ReportsRegister>(jsonString);

            if (creports01?.Report == null)
            {
                return [];
            }

            var resultreports01 = creports01.Report.Where(d => true);
            return resultreports01;
        }

        public IEnumerable<Lakehouse> DeserializeLakehouse(string jsonString)
        {
            var lakehouses = JsonSerializer.Deserialize<LakehousesRegister>(jsonString);

            if (lakehouses?.Lakehouses == null)
            {
                return [];
            }

            foreach (var lakehouse in lakehouses.Lakehouses)
            {
                lakehouse.DefaultSchema = lakehouse.Properties?.DefaultSchema;
                lakehouse.OneLakeTablesPath = lakehouse.Properties?.OneLakeTablesPath;
                lakehouse.OneLakeFilesPath = lakehouse.Properties?.OneLakeFilesPath;
                lakehouse.SqlEndpointId = lakehouse.Properties?.SqlEndpointProperties?.Id;
                lakehouse.SqlEndpointConnectionString = lakehouse.Properties?.SqlEndpointProperties?.ConnectionString;
                lakehouse.SqlEndpointProvisioningStatus = lakehouse.Properties?.SqlEndpointProperties?.ProvisioningStatus;
            }

            return lakehouses.Lakehouses;
        }

        public IEnumerable<Warehouse> DeserializeWarehouse(string jsonString)
        {
            var warehouses = JsonSerializer.Deserialize<WarehousesRegister>(jsonString);

            if (warehouses?.Warehouses == null)
            {
                return [];
            }

            foreach (var warehouse in warehouses.Warehouses)
            {
                warehouse.ConnectionString = warehouse.Properties?.ConnectionString;
                warehouse.CreatedDate = warehouse.Properties?.CreatedDate;
                warehouse.LastUpdatedTime = warehouse.Properties?.LastUpdatedTime;
                warehouse.CollationType = warehouse.Properties?.CollationType;
            }

            return warehouses.Warehouses;
        }
    }
}
