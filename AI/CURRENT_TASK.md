# CURRENT TASK

## Goal
Implement a real compact redesign of the top information/tools area in FacilityWorkbench and simplify editor/hover metadata UI.

## Problem
The previous pass did not produce a real redesign.
The top area still visually blends together, the hierarchy is weak, the interval range is too small, the selection summary is badly composed, and the tools block still feels like the old panel with a few things removed.

Current issues:
- Interval / Selection / Tools are not visually separated enough
- the selected interval range is too small and visually weak
- the selection summary donut/card has poor composition
- the tools area still does not feel like a compact card-based redesign
- low-value status rows must remain removed
- hover card is acceptable and should only receive the final Note/Style cleanup direction already decided

## Desired direction
Deliver a materially new redesign built as 3 clearly separated cards:

1. Interval card
2. Selection summary card
3. Tools card

And additionally:
- remove Tags from active UI / active functionality
- remove Editor Tags section
- keep Hover card compact with Note instead of Tags
- keep the whole redesigned area more compact and more readable on Full HD

## Scope
Implementation only for:
- top info/tools box redesign
- removal of Tags from active UI / active flow
- removal of Editor Tags section
- hover card Note + Style display-name cleanup

## Do NOT change
- Do not reopen Interval logic/semantics beyond the visual redesign
- Do not redesign the whole page outside this area
- Do not redesign analytics tabs/content
- Do not change graph/database/topology architecture
- Do not perform browser validation
- Do not use browser automation
- Do not do broad persisted tag cleanup/migration

## Constraints
- The result must feel like a new layout, not just reduced spacing
- Cards must be visibly separated with borders/radius/background contrast
- The selected interval range must become visually prominent
- The selection summary must be re-composed with proper KPI + donut structure
- The tools card must be genuinely card-based and compact
- Build/compile validation only; provide manual validation checklist for the user

## Guardrails
- No fake redesign through small spacing tweaks
- No labels inside donut slices
- No large legend block for the donut
- No reintroduction of low-value status rows
- Tags must not remain in active filters/editor
- Keep hover card compact and read-only