# Current Task

## Goal
Implement phase 1 of the refined FacilityWorkbench data-presentation cleanup.

## Problem
The refined plan is approved.
The current FacilityWorkbench is overloaded, renders too much at once, and some interactions are slow.

Before adding new literature-backed visualizations, I want to:
- simplify the workbench
- split analytics into tabs
- reduce eager rendering
- remove the current topbar alert dropdown
- prepare a cleaner base for future KPI/visualization additions

## Scope
Implement only the first structural cleanup slice.

### Required in this phase
1. Add a tab-based analytics structure inside FacilityWorkbench.
2. Make only the Overview tab eager/default.
3. Make other analytics tabs lazy/conditional.
4. Remove the current topbar alert dropdown.
5. Reassign current widgets into tabs.

### Suggested tab structure for this phase
- Overview
- Breakdown
- Performance
- Compare
- Diagnostics

### Keep Overview focused on
- Headline KPI
- main time-series chart
- baseline/deviation status
- essential context only

### Move into Breakdown
- Disaggregation / Top Contributors
- Role Breakdown
- Source Map

### Move into Performance
- Load Profile
- Peak Analysis
- Operating Regime

### Move into Compare
- Compare Set manager
- compare chart

### Move into Diagnostics
- Forecast vs Actual
- Forecast diagnostics

## Do NOT implement yet
- load duration curve
- scatter temperature vs load
- benchmark / peer comparison
- EUI / cost per m²
- larger analytics-method redesign
- topology/editor/layout redesign

## Constraints
- Work only on FacilityWorkbench as the primary target surface
- Treat legacy pages as legacy/reference only
- Keep the solution focused on data presentation and rendering behavior
- Prefer removing clutter over preserving every current widget in the main default view
- Topbar alerts should be removed in this phase, not redesigned yet

## Acceptance criteria
- FacilityWorkbench analytics are split into tabs
- only Overview is eager/default
- other tabs are lazy/conditional
- topbar alert dropdown is removed
- current widgets are redistributed into the new tabs
- the default view is visibly simpler and lighter