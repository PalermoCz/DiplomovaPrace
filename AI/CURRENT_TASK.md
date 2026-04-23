# Current Task

## Goal
Implement phase 1 of the new literature-backed Performance tab model in FacilityWorkbench.

## Problem
The approved plan is:
- keep Performance as a compact demand-performance workspace
- replace overly heuristic-heavy content
- introduce clearer and more defensible KPI presentation

The first implementation slice should focus on:
- peak demand
- load factor
- after-hours / night / weekend load

## Scope
Implement only the first Performance tab evolution slice.

### Keep as base
- Peak Analysis as the main starting point for peak-demand detail

### Replace / simplify
- replace Operating Regime with clearer KPI-based presentation
- demote or simplify Load Profile instead of keeping it as a primary widget

### New KPI focus
- Peak Demand
- Load Factor
- After-hours / Night / Weekend Load

## Constraints
- Work only on FacilityWorkbench Performance tab
- Do not redesign topology/editor/layout
- Do not add scatter, load duration curve, benchmark, EUI, or cost per m² yet
- Keep the solution general
- Prefer clear KPI presentation over heuristic summaries

## Acceptance criteria
- Performance tab clearly presents:
  - peak demand
  - load factor
  - after-hours / night / weekend load
- Operating Regime is no longer the primary model
- Load Profile is simplified or demoted
- Peak Analysis remains as a supporting detail surface
- result fits the current tabbed workbench structure