using DiplomovaPrace.Models.Kpi;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using System.IO;

namespace DiplomovaPrace.Services;

public class CuratedNodeSummary
{
    public string Title { get; set; } = string.Empty;
    public double TotalSum { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int DataPoints { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class NodeAnalyticsPreviewService
{
    private readonly IKpiService _kpiService;
    private readonly IWebHostEnvironment _env;

    public NodeAnalyticsPreviewService(IKpiService kpiService, IWebHostEnvironment env)
    {
        _kpiService = kpiService;
        _env = env;
    }

    public async Task<MeterKpiResult?> GetPreviewDataAsync(string meterUrn, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var query = new KpiQuery(meterUrn, from, to);
        var result = await _kpiService.CalculateBasicKpiAsync(query, ct);

        // Pokud nemáme žádná data, vracíme null
        if (result.RecordCount == 0)
            return null;

        return result;
    }

    public async Task<CuratedNodeSummary?> GetCuratedSummaryAsync(string nodeKey, DateTime from, DateTime to, CancellationToken ct = default)
    {
        string? fileName = null;
        string? columnName = null;
        string unit = "kWh";
        string title = "Souhrn";

        switch (nodeKey)
        {
            case "pv_main":
                fileName = "electricity_P.csv";
                columnName = "PV";
                title = "Solární výroba (PV)";
                break;
            case "chp_main":
                fileName = "electricity_P.csv";
                columnName = "CHP";
                title = "Výroba kogenerace (CHP)";
                break;
            case "cooling_main":
                fileName = "cooling_P.csv";
                columnName = "total";
                title = "Celkové chlazení";
                break;
            case "heating_main":
                fileName = "heating_P.csv";
                columnName = "total";
                title = "Celkové vytápění";
                break;
            case "weather_main":
                fileName = "weather.csv";
                columnName = "WeatherStation.Weather.Ta";
                title = "Okamžitá průměrná teplota";
                unit = "°C";
                break;
            default:
                return null;
        }

        // Pokus o nalezení souboru v lokální DataSet struktuře (přednost má složka curated)
        var filePath = Path.Combine(_env.ContentRootPath, "..", "DataSet", "curated", fileName);
        if (!File.Exists(filePath))
        {
             filePath = Path.Combine(_env.ContentRootPath, "..", "DataSet", fileName);
             if (!File.Exists(filePath)) return null;
        }

        return await ParseCsvColumnAsync(filePath, columnName, title, unit, from, to, ct);
    }

    private async Task<CuratedNodeSummary?> ParseCsvColumnAsync(string filePath, string columnName, string title, string unit, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var headerLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(headerLine)) return null;

            var headers = headerLine.Split(',');
            int colIndex = Array.FindIndex(headers, h => h.Trim().Equals(columnName, StringComparison.OrdinalIgnoreCase));
            int timeColIndex = Array.FindIndex(headers, h => 
                h.Trim().Equals("datetime_utc", StringComparison.OrdinalIgnoreCase) || 
                h.Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase) || 
                h.Trim().Equals("time", StringComparison.OrdinalIgnoreCase));
            
            if (colIndex == -1) return null;
            if (timeColIndex == -1) timeColIndex = 0; // fallback to first column

            double sum = 0;
            int count = 0;
            double min = double.MaxValue;
            double max = double.MinValue;

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                var cols = line.Split(',');
                if (cols.Length > timeColIndex && cols.Length > colIndex)
                {
                    if (DateTime.TryParse(cols[timeColIndex], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
                    {
                        if (timestamp >= from && timestamp < to)
                        {
                            if (double.TryParse(cols[colIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                            {
                                sum += val;
                                count++;
                                if (val < min) min = val;
                                if (val > max) max = val;
                            }
                        }
                        
                        // Zastaví čtení pokud jsme chronologicky přesáhli zkoumané období (zlepšuje výkon pro starší data)
                        if (timestamp >= to)
                        {
                            break;
                        }
                    }
                }
            }

            if (count == 0) return null;

            return new CuratedNodeSummary
            {
                Title = title,
                TotalSum = sum,
                Average = sum / count,
                Min = min,
                Max = max,
                DataPoints = count,
                Unit = unit
            };
        }
        catch
        {
            return null; // Při file I/O problému prostě fallbackujeme do NO-DATA stavu
        }
    }
}
