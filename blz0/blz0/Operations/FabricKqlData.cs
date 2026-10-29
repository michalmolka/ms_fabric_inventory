using blz0.Models;
using static blz0.Authorization.FabricAuth;
using Kusto.Data.Net.Client;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using FSharpEventLogProcessing = EventLogProcessing;
using System.Runtime.InteropServices;

namespace blz0.Operations
{
    public class FabricKqlData
    {
        public IEnumerable<EventLogs>? queryResult { get; set; }
        public List<EventLogsCount>? filledLogs { get; set; }

        public List<AnomalyPrediction>? anomalyPredictions { get; set; }
        public FSharpEventLogProcessing.EventLogInsightsResult? EventLogInsights { get; set; }
        public string? AnomalyDetectionInfo { get; set; }
        public async Task GetEventLogs(bool apiSource, string query = "", int takeValue = 1000, string agoValue = "5d")
        {
            if (query == "")
            {
                query = "['ms-fabric-logs']| take 10000| project Id = id, EventTime = ['time'], EventType = ['type'], ItemId = data['itemId'], ItemName = data['itemName'], WorkspaceId = data['workspaceId'], WorkspaceName = data['workspaceName'], ItemKind = data['itemKind']|filter EventTime >= ago(10d)";
            }
            FabricAuthKQL fabricAuthKQL = new FabricAuthKQL();
            await fabricAuthKQL.GetFabricAuthKQL();
            var results = new List<EventLogs>();
            using (var client = KustoClientFactory.CreateCslQueryProvider(fabricAuthKQL.kustoAuth))
            {
                System.Data.IDataReader? reader;// = client.ExecuteQuery(query);

                if (apiSource)
                {
                    reader = client.ExecuteQuery("['ms-fabric-logs']| take " + takeValue.ToString() + "| project Id = id, EventTime = ['time'], EventType = ['type'], ItemId = data['itemId'], ItemName = data['itemName'], WorkspaceId = data['workspaceId'], WorkspaceName = data['workspaceName'], ItemKind = data['itemKind']|filter EventTime >= ago(" + agoValue.ToString() + ")");
                }
                else
                {
                    reader = client.ExecuteQuery(query);
                }

                while (reader.Read())
                {
                    var result = new EventLogs
                    {
                        Id = reader["Id"].ToString(),
                        EventTime = Convert.ToDateTime(reader["EventTime"]),
                        EventType = reader["EventType"].ToString(),
                        ItemId = reader["ItemId"].ToString(),
                        ItemName = reader["ItemName"].ToString(),
                        WorkspaceId = reader["WorkspaceId"].ToString(),
                        WorkspaceName = reader["WorkspaceName"].ToString(),
                        ItemKind = reader["ItemKind"].ToString()
                    };
                    results.Add(result);
                }
                this.queryResult = results.AsEnumerable();
            }
        }

        public async Task EventLogAggCount()
        {
            Console.WriteLine("EventLogsAggCount");

            if (this.queryResult == null)
            {
                await GetEventLogs(false);
            }

            List<EventLogsCount> EventLogsAggCount = this.queryResult!
                .GroupBy(i => i.EventTime!.Value.Date)
                .Select(g => new EventLogsCount
                {
                    EventTime = DateOnly.FromDateTime(g.Key),
                    Cnt = g.Count()
                })
                .ToList();

            if (EventLogsAggCount.Count == 0)
            {
                this.filledLogs = [];
                UpdateEventLogInsights();
                return;
            }

#pragma warning disable CS8629 // Nullable value type may be null.
            DateOnly startDate = EventLogsAggCount.Where(e => e.EventTime.HasValue).Min(e => e.EventTime.Value);
#pragma warning restore CS8629 // Nullable value type may be null.
            DateOnly endDate = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);

            var allDates = Enumerable.Range(0, endDate.DayNumber - startDate.DayNumber + 1)
                                     .Select(offset => startDate.AddDays(offset))
                                     .ToList();

            var filledLogs = allDates
                .Select(date => EventLogsAggCount.FirstOrDefault(e => e.EventTime == date) ??
                                new EventLogsCount { EventTime = date, Cnt = 0 })
                .ToList();

            this.filledLogs = filledLogs.OrderBy(e => e.EventTime).ToList();
            UpdateEventLogInsights();
        }

        public async Task EventLogsAnomalyDetection()
        {
            AnomalyDetectionInfo = null;
            // Always rebuild daily aggregates from the latest query result before anomaly scoring.
            await EventLogAggCount();

            var context = new MLContext();
            var preprocessedLogs = this.filledLogs!.Select(log => new EventLogsCountAnomaly
            {
                TimeStamp = log.EventTime?.ToDateTime(TimeOnly.MinValue) ?? DateTime.MinValue, // Convert DateOnly to DateTime, handle null
                Value = (float)log.Cnt // Cast int to float
            }).ToList();

            //    var preprocessedData = new List<EventLogsCountAnomaly>
            //{
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-1), Value = 1200 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-2), Value = 1500 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-3), Value = 2000 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-4), Value = 1200 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-5), Value = 2000 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-6), Value = 800 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-7), Value = 300 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-8), Value = 20 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-9), Value = 4000 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-10), Value = 1500 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-11), Value = 1400 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-12), Value = 800 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-13), Value = 200 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-14), Value = 11400 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-15), Value = 1600 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-16), Value = 2200 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-17), Value = 2400 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-18), Value = 300 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-19), Value = 900 },
            //    new EventLogsCountAnomaly { TimeStamp = DateTime.Now.AddDays(-20), Value = 1400 }
            //};

            // Simulated data for demonstration purposes

            var dataView = context.Data.LoadFromEnumerable(preprocessedLogs);

            try
            {
                IEstimator<ITransformer> pipeline;
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    // ARM64-safe ML.NET detector (doesn't rely on SSA FFT/MKL native path).
                    pipeline = context.Transforms.DetectIidSpike(
                        outputColumnName: nameof(AnomalyPrediction.Prediction),
                        inputColumnName: nameof(EventLogsCountAnomaly.Value),
                        confidence: 99.0,
                        pvalueHistoryLength: Math.Max(8, preprocessedLogs.Count / 3));
                    AnomalyDetectionInfo = "ML.NET IID spike detector is used on ARM64 runtime.";
                }
                else
                {
                    pipeline = context.Transforms.DetectSpikeBySsa(
                        outputColumnName: nameof(AnomalyPrediction.Prediction),
                        inputColumnName: nameof(EventLogsCountAnomaly.Value),
                        confidence: 99.0,
                        pvalueHistoryLength: preprocessedLogs.Count / 4,
                        trainingWindowSize: preprocessedLogs.Count,
                        seasonalityWindowSize: preprocessedLogs.Count / 4);
                }

                var model = pipeline.Fit(dataView);
                var transformedData = model.Transform(dataView);
                this.anomalyPredictions = context.Data.CreateEnumerable<AnomalyPrediction>(transformedData, reuseRowObject: false).ToList();
            }
            catch (Exception ex) when (
                ex is DllNotFoundException ||
                ex is TypeInitializationException)
            {
                // Fallback for environments where ML.NET SSA native FFT/MKL dependency is unavailable (e.g., linux-arm64 dev containers).
                this.anomalyPredictions = ComputeFallbackAnomalies(preprocessedLogs);
                AnomalyDetectionInfo = "Fallback anomaly detection is active (SSA/MKL unavailable in current runtime).";
            }

            UpdateEventLogInsights();
            await Task.CompletedTask;
            //Console.WriteLine("Time\t\tValue\tAnomaly");
            //for (int i = 0; i < this.anomalyPredictions.Count; i++)
            //{
            //    var sensorData = this.filledLogs[i];
            //    var prediction = this.anomalyPredictions[i];
            //    var isAnomaly = prediction.Prediction[0] == 1;
            //    Console.WriteLine($"{sensorData.EventTime}\t{sensorData.Cnt}\t{isAnomaly}");
            //}


        }

        private static List<AnomalyPrediction> ComputeFallbackAnomalies(List<EventLogsCountAnomaly> series)
        {
            if (series.Count == 0)
            {
                return [];
            }

            var values = series.Select(e => (double)e.Value).ToList();
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            var threshold = stdDev <= 0.0001 ? double.MaxValue : 3.0 * stdDev;

            return values
                .Select(v =>
                {
                    var isAnomaly = Math.Abs(v - mean) > threshold;
                    return new AnomalyPrediction
                    {
                        Prediction = [isAnomaly ? 1f : 0f, 0f, 0f]
                    };
                })
                .ToList();
        }

        private void UpdateEventLogInsights()
        {
            if (this.filledLogs == null)
            {
                this.EventLogInsights = null;
                return;
            }

            var dates = this.filledLogs
                .Select(e => e.EventTime?.ToString("yyyy-MM-dd") ?? "-")
                .ToList();
            var counts = this.filledLogs
                .Select(e => e.Cnt)
                .ToList();
            var anomalyFlags = this.filledLogs
                .Select((_, i) =>
                {
                    if (this.anomalyPredictions == null || i >= this.anomalyPredictions.Count)
                    {
                        return false;
                    }

                    var prediction = this.anomalyPredictions[i]?.Prediction;
                    return prediction is { Length: > 0 } && prediction[0] == 1;
                })
                .ToList();

            var insights = new FSharpEventLogProcessing.EventLogInsights();
            this.EventLogInsights = insights.Analyze(dates, counts, anomalyFlags);
        }
    }
}
