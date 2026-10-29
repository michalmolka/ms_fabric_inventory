namespace blz0.Models
{
    public class DashboardStats
    {
        public int ActiveCapacities { get; set; }
        public int ActiveWorkspaces { get; set; }
        public int ActiveDatasets { get; set; }
        public int ActiveReports { get; set; }
        public int InactiveCapacities { get; set; }
        public int InactiveWorkspaces { get; set; }
        public int InactiveDatasets { get; set; }
        public int InactiveReports { get; set; }
        public List<DashboardSlice> WorkspacesByCapacity { get; set; } = [];
        public List<DashboardSlice> DatasetsByWorkspace { get; set; } = [];
        public List<DashboardSlice> ReportsByWorkspace { get; set; } = [];
        public List<DashboardSlice> DatasetStorageModes { get; set; } = [];
        public List<DashboardSlice> ReportTypes { get; set; } = [];

        public int ActiveTotal => ActiveCapacities + ActiveWorkspaces + ActiveDatasets + ActiveReports;
        public int InactiveTotal => InactiveCapacities + InactiveWorkspaces + InactiveDatasets + InactiveReports;
    }

    public class DashboardSlice
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
    }
}
