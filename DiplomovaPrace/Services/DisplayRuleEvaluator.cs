namespace DiplomovaPrace.Services;

using DiplomovaPrace.Models;
using DiplomovaPrace.Models.Configuration;

/// <summary>
/// Vyhodnocuje DisplayRules a vrací barvu prvního splněného pravidla.
/// Obsahuje caching proč ExpressionEvaluator je single-threaded a synchronní.
/// </summary>
public interface IDisplayRuleEvaluator
{
    /// <summary>
    /// Najde první splněné DisplayRule a vrací jeho barvu.
    /// Pokud žádné pravidlo není splněno, vrací null (použije se výchozí barva).
    /// </summary>
    Task<string?> EvaluateAsync(
        IReadOnlyList<DisplayRule> rules,
        string buildingId);
}

public class DisplayRuleEvaluator : IDisplayRuleEvaluator
{
    private readonly IBuildingStateService _stateService;
    private readonly ExpressionEvaluator _expressionEvaluator;

    public DisplayRuleEvaluator(
        IBuildingStateService stateService,
        ExpressionEvaluator expressionEvaluator)
    {
        _stateService = stateService;
        _expressionEvaluator = expressionEvaluator;
    }

    public async Task<string?> EvaluateAsync(
        IReadOnlyList<DisplayRule> rules,
        string buildingId)
    {
        if (rules == null || rules.Count == 0)
            return null;

        foreach (var rule in rules)
        {
            var (result, error) = await _expressionEvaluator.EvaluateAsync(rule.Condition, buildingId);

            // Chyba se tiše ignoruje (používá se výchozí barva)
            if (error != null)
                continue;

            // Pokud výsledek je "true" (1.0), vratiš barvu pravidla
            if (result.HasValue && Math.Abs(result.Value - 1.0) < 0.0001)
                return rule.Color;
        }

        return null;  // Žádné pravidlo splněno
    }
}
