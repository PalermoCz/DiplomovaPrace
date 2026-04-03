using DiplomovaPrace.Models.Kpi;

namespace DiplomovaPrace.Services;

public interface IPortfolioAnalyticsService
{
    Task<PortfolioBenchmarkResult> GetPortfolioBenchmarkAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
