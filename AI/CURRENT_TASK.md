# CURRENT_TASK.md

## Název úkolu
FacilityWorkbench — přesný replace-only fix pro 3 chyby

Tento task je čistě REPLACE-ONLY.
Neimprovizuj.
Nevymýšlej alternativní řešení.
Neprováděj refactor.
Nedělej další změny mimo přesně uvedené bloky.

Uprav pouze:
- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor`
- `DiplomovaPrace/Components/Pages/FacilityWorkbench.razor.css`

Nepoužívej service refactor.
Nesahej do `NodeAnalyticsPreviewService.cs`.
Nesahej do treemapy, editoru, schematicu, loading railu ani jiné části aplikace.

---

# Cíl
Opravit přesně tyto 3 chyby:

1. `Selected / With data / No data` musí být interně konzistentní
2. první výběr nodeů musí správně načíst Overview
3. tabs strip `Trend / Baseline / Scatter / Power / EUI` musí být skutečně samostatný bullet a nesmí sdílet ohraničení s `Data Source + Signal`

---

# KROK 1 — Oprava selection summary classification

## Najdi v `FacilityWorkbench.razor` tuto metodu:
```csharp
private SelectionSummaryClassificationSnapshot BuildSelectionSummaryClassificationSnapshot()
````

## Smaž CELÉ aktuální tělo této metody

a nahraď ho přesně tímto:

```csharp
private SelectionSummaryClassificationSnapshot BuildSelectionSummaryClassificationSnapshot()
{
    var selectedNodeKeys = _selectionOrder
        .Where(_selectionSet.Contains)
        .Where(nodeKey => !string.IsNullOrWhiteSpace(nodeKey))
        .Where(nodeKey => !IsWeatherNode(nodeKey))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (selectedNodeKeys.Count == 0)
    {
        return new SelectionSummaryClassificationSnapshot([], [], [], []);
    }

    var supportedItems = SelectionSummaryItems
        .Where(item => selectedNodeKeys.Contains(item.NodeKey, StringComparer.OrdinalIgnoreCase))
        .Where(IsLeafOrAnalyticNode)
        .Where(item => !FacilityNodeSemantics.IsWeatherContextNode(item.NodeKey))
        .ToList();

    var supportedNodeKeys = supportedItems
        .Select(item => item.NodeKey)
        .Where(nodeKey => !string.IsNullOrWhiteSpace(nodeKey))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    var withDataNodeKeys = selectedNodeKeys
        .Where(NodeHasAnyAnalysisData)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    var withDataNodeKeySet = withDataNodeKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

    var noDataNodeKeys = selectedNodeKeys
        .Where(nodeKey => !withDataNodeKeySet.Contains(nodeKey))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    return new SelectionSummaryClassificationSnapshot(
        supportedItems,
        supportedNodeKeys,
        withDataNodeKeys,
        noDataNodeKeys);
}
```

***

# KROK 2 — Oprava aggregate scope pro Overview

## Najdi v `FacilityWorkbench.razor` tuto metodu:

```csharp
private IReadOnlyList<string> GetSelectionAggregateNodeKeys()
```

## Smaž CELÉ aktuální tělo této metody

a nahraď ho přesně tímto:

```csharp
private IReadOnlyList<string> GetSelectionAggregateNodeKeys()
{
    return _selectionOrder
        .Where(_selectionSet.Contains)
        .Where(nodeKey => !string.IsNullOrWhiteSpace(nodeKey))
        .Where(nodeKey => !IsWeatherNode(nodeKey))
        .Where(NodeHasAnyAnalysisData)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}
```

***

# KROK 3 — Oprava unavailable state property

## Najdi v `FacilityWorkbench.razor` tento řádek:

```csharp
private bool ShouldShowOverviewUnavailableState => !ShouldShowOverviewLoadingSkeleton && IsOverviewAvailabilityDecisionReady() && ShouldShowOverviewUnavailable();
```

## Nahraď ho přesně tímto:

```csharp
private bool ShouldShowOverviewUnavailableState => !ShouldShowOverviewLoadingSkeleton && ShouldShowOverviewUnavailable();
```

***

# KROK 4 — Oprava `ShouldShowOverviewUnavailable()`

## Najdi v `FacilityWorkbench.razor` tuto metodu:

```csharp
private bool ShouldShowOverviewUnavailable()
```

## Smaž CELÉ aktuální tělo této metody

a nahraď ho přesně tímto:

```csharp
private bool ShouldShowOverviewUnavailable()
{
    if (!HasSelectedNodes)
    {
        return false;
    }

    var aggregateScopeNodeKeys = GetSelectionAggregateNodeKeys();
    if (aggregateScopeNodeKeys.Count == 0)
    {
        return true;
    }

    return _selectionAggregateOverview is null || _selectionAggregateOverview.Summary is null;
}
```

***

# KROK 5 — Oprava `ResolveOverviewUnavailableMessage()`

## Najdi v `FacilityWorkbench.razor` tuto metodu:

```csharp
private string ResolveOverviewUnavailableMessage()
```

## Smaž CELÉ aktuální tělo této metody

a nahraď ho přesně tímto:

```csharp
private string ResolveOverviewUnavailableMessage()
{
    var aggregateScopeNodeKeys = GetSelectionAggregateNodeKeys();
    if (aggregateScopeNodeKeys.Count == 0)
    {
        return "The current selection has no nodes with compatible analytics data for aggregate overview.";
    }

    if (!string.IsNullOrWhiteSpace(_selectionAggregateOverview?.Message))
    {
        return _selectionAggregateOverview.Message;
    }

    return "Aggregate overview data is unavailable for the current interval.";
}
```

***

# KROK 6 — Nech `Selected` jako skutečný `SelectionCount`

Tento krok je kontrolní.

## V `FacilityWorkbench.razor` najdi tento blok:

```razor
<div class="cp-sel-donut-center" aria-hidden="true">
    <div class="cp-sel-count-big">@SelectionCount</div>
    <div class="cp-sel-count-label">Selected</div>
</div>
```

## Pokud je tam jiný výraz než `@SelectionCount`, vrať ho přesně na tento blok:

```razor
<div class="cp-sel-donut-center" aria-hidden="true">
    <div class="cp-sel-count-big">@SelectionCount</div>
    <div class="cp-sel-count-label">Selected</div>
</div>
```

***

# KROK 7 — Oprava wrapperu Analysis části, aby tabs strip nebyl ve stejné outer card

## V `FacilityWorkbench.razor` najdi tento řádek:

```razor
<div class="overview-widget overview-widget-soft analysis-workspace-shell">
```

## Nahraď ho přesně tímto:

```razor
<div class="analysis-workspace-shell">
```

***

# KROK 8 — Oprava CSS pro skutečně samostatný tabs bullet

## V `FacilityWorkbench.razor.css` najdi tento blok:

```css
.analysis-workspace-shell {
    border-style: solid;
    gap: 0.72rem;
}
```

## Nahraď ho přesně tímto:

```css
.analysis-workspace-shell {
    display: flex;
    flex-direction: column;
    gap: 0.72rem;
}
```

***

## V `FacilityWorkbench.razor.css` najdi tento blok:

```css
.analysis-module-strip {
    margin-top: 0.18rem;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.42rem;
    width: 100%;
    padding: 0.48rem 0.56rem;
    border: 1px solid var(--wb-border);
    border-radius: 0.95rem;
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.95) 0%, rgba(248, 251, 255, 0.96) 100%);
    box-shadow: var(--wb-shadow-soft);
}
```

## Nahraď ho přesně tímto:

```css
.analysis-module-strip {
    margin-top: 0.72rem;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.42rem;
    width: auto;
    max-width: 100%;
}
```

***

## V `FacilityWorkbench.razor.css` najdi tento blok:

```css
.analysis-control-tabs-row {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    margin-top: 0;
    width: 100%;
}
```

## Nahraď ho přesně tímto:

```css
.analysis-control-tabs-row {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    margin-top: 0;
}
```

***

## V `FacilityWorkbench.razor.css` najdi tento blok:

```css
.analysis-module-nav {
    width: auto;
    max-width: 100%;
    border-color: #d7e2f1;
    background: rgba(255, 255, 255, 0.9);
}
```

## Nahraď ho přesně tímto:

```css
.analysis-module-nav {
    width: auto;
    max-width: 100%;
}
```

***

# KROK 9 — Build a nic dalšího

Po všech replace:

1.  build
2.  nic dalšího neupravuj
3.  neposílej další návrhy

***

# Akceptační kritéria

Task je hotový jen pokud:

1.  Selection karta ukazuje:
    *   `Selected = skutečný SelectionCount`
2.  Pro selection typu `19 selected` už nevznikne stav:
    *   `16 with data`
    *   `2 no data`
3.  První výběr nodeů už neskončí blank / chybným overview stavem
4.  `Overview unavailable` se zobrazí jen když aggregate scope opravdu neexistuje nebo overview result opravdu není k dispozici
5.  `Trend / Baseline / Scatter / Power / EUI` je vizuálně samostatný bullet a nesdílí outer border s `Data Source + Signal`
6.  Build projde

***

# Povinný výstup agenta

Na konci napiš:

1.  že jsi přesně nahradil `BuildSelectionSummaryClassificationSnapshot()`
2.  že jsi přesně nahradil `GetSelectionAggregateNodeKeys()`
3.  že jsi přesně nahradil `ShouldShowOverviewUnavailableState`
4.  že jsi přesně nahradil `ShouldShowOverviewUnavailable()`
5.  že jsi přesně nahradil `ResolveOverviewUnavailableMessage()`
6.  že jsi přesně nahradil Analysis wrapper a CSS bloky pro tabs strip
7.  jestli build prošel

Pokud toto nevyjmenuješ, task je nesplněný.

````

---

# Krátký prompt pro agenta

```text
Otevři CURRENT_TASK.md a udělej přesně jen uvedené replace operace.

Důležité:
- nic nevymýšlej
- nic nerefactoruj
- nic neopravuj bokem
- jen najdi přesné bloky a nahraď je přesně zadaným kódem
- scope:
  - FacilityWorkbench.razor
  - FacilityWorkbench.razor.css

Na konci povinně napiš:
1. které bloky jsi nahradil
2. že jsi nic dalšího neměnil
3. jestli build prošel