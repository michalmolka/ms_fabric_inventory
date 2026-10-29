namespace EventLogProcessing

open System

[<CLIMutable>]
type EventLogInsightsResult =
    {
        Trend: string
        TrendDescription: string
        AnomalySummary: string
        StrongestAnomaly: string
        Last7DaysChange: string
        DataQuality: string
    }

type EventLogInsights() =

    member _.Analyze(dates: seq<string>, counts: seq<int>, anomalyFlags: seq<bool>) : EventLogInsightsResult =
        let formatSigned (value: float) =
            if value >= 0.0 then sprintf "+%.2f" value else sprintf "%.2f" value

        let rows =
            Seq.zip3 dates counts anomalyFlags
            |> Seq.map (fun (date, count, anomaly) -> {| Date = date; Count = count; Anomaly = anomaly |})
            |> Seq.toList

        if List.isEmpty rows then
            {
                Trend = "No data"
                TrendDescription = "There is no event log series to analyze."
                AnomalySummary = "No anomalies can be evaluated."
                StrongestAnomaly = "-"
                Last7DaysChange = "-"
                DataQuality = "No data"
            }
        else
            let countValues = rows |> List.map (fun row -> row.Count)
            let average = countValues |> List.averageBy float
            let zeroDays = rows |> List.filter (fun row -> row.Count = 0) |> List.length
            let anomalyRows = rows |> List.filter (fun row -> row.Anomaly)

            let firstWindow, lastWindow =
                if rows.Length >= 14 then
                    rows |> List.take 7, rows |> List.skip (rows.Length - 7)
                else
                    let midpoint = max 1 (rows.Length / 2)
                    rows |> List.take midpoint, rows |> List.skip midpoint

            let firstAverage =
                if List.isEmpty firstWindow then 0.0 else firstWindow |> List.averageBy (fun row -> float row.Count)

            let lastAverage =
                if List.isEmpty lastWindow then firstAverage else lastWindow |> List.averageBy (fun row -> float row.Count)

            let changePercent =
                if abs firstAverage < 0.0001 then
                    0.0
                else
                    ((lastAverage - firstAverage) / firstAverage) * 100.0

            let trend =
                if changePercent > 10.0 then "Increasing"
                elif changePercent < -10.0 then "Decreasing"
                else "Stable"

            let trendDescription =
                sprintf "%s trend. Recent average %.2f vs earlier average %.2f." trend lastAverage firstAverage

            let anomalySummary =
                match anomalyRows.Length with
                | 0 -> "No anomaly days detected."
                | 1 -> "1 anomaly day detected."
                | value -> sprintf "%i anomaly days detected." value

            let strongestAnomaly =
                anomalyRows
                |> List.sortByDescending (fun row -> abs (float row.Count - average))
                |> List.tryHead
                |> function
                    | Some row ->
                        let delta = float row.Count - average
                        sprintf "%s (%s vs avg)" row.Date (formatSigned delta)
                    | None -> "-"

            let last7DaysChange =
                if rows.Length < 2 then
                    "-"
                else
                    sprintf "%s%%" (formatSigned changePercent)

            let dataQuality =
                let zeroRate = (float zeroDays / float rows.Length) * 100.0
                if rows.Length < 14 then
                    sprintf "Limited: only %i days in series." rows.Length
                elif zeroRate > 30.0 then
                    sprintf "Warning: %.1f%% days have zero events." zeroRate
                else
                    "Good"

            {
                Trend = trend
                TrendDescription = trendDescription
                AnomalySummary = anomalySummary
                StrongestAnomaly = strongestAnomaly
                Last7DaysChange = last7DaysChange
                DataQuality = dataQuality
            }
