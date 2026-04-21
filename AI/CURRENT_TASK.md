# Current Task

## Goal
Dokončit group drag bugfix v editoru.

## Problem
Group drag stále nefunguje, i když single/multi selection, panel split, save/undo a single-click -> Edit už fungují.

Po ručním průchodu aktuálního kódu jsou nyní hlavní podezřelé body:

1. JS group-drag activation je plně závislá na `_selectedNodeKeys` při `onMouseDown`.
   Pokud JS selected-set není v ten okamžik správně synchronizovaný, kód spadne do single-drag větve místo group-drag větve.

2. `groupEls` se v `editor.js` skládají přes globální:
   `document.querySelectorAll('g[data-node-key]')`
   místo aby se omezily jen na aktuální schematic / current SVG content root.
   To může znamenat, že group drag pracuje se špatnými elementy nebo smíšenými instancemi.

Původní click-collapse bug už pravděpodobně není hlavní problém, protože `groupDrag` mouse-up už nastavuje `_suppressNextClick = true` bezpodmínečně.

## Expected behavior
- box-select více node
- chytnu jeden z vybraných node
- bez modifier key se pohne celá selected group
- group drag se rozhoduje správně už při drag startu
- group move pracuje jen s node z aktuálního schematic instance
- relativní pozice group se zachovají
- dirty/save/undo flow zůstane funkční
- single-click -> Edit musí zůstat funkční

## Constraints
- Keep solution narrow
- Do not reopen Home/root logic
- Do not reopen grid extent logic
- Do not break current correct single-vs-multi panel behavior
- Prefer root-cause fix, not workaround
- Reuse existing dirty/save/undo flow

## Acceptance criteria
- dragging one selected node moves the whole selected group
- no modifier key is needed
- group drag uses only nodes from the active schematic instance
- group drag does not silently fall back to single drag when multi-selection is active
- relative spacing stays preserved
- save/undo still works
- single-click -> Edit still works