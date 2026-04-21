# Current Task

## Goal
Opravit editor UX pro multi-select, group drag, pravý panel, Home a viewport/grid chování tak, aby editor fungoval konzistentně a obecně.

## Problem
Po posledních změnách je editor blízko správnému stavu, ale stále má několik chyb:

1. Group drag stále nefunguje správně.
2. Po rectangle multi-selectu se někdy zvýrazní i nesouvisející uzel mimo skutečný selected set.
   Konkrétní pozorování:
   - při výběru `pv_roof_branch` + jeho 3 child node (celkem 4 uzly) se světle modře zvýrazní i `Electricity`, což se dít nemá
   - unrelated node mimo selected set nesmí být vizuálně označen jako selected / semi-selected / focused
3. Multi-select a single-select UI logika stále není správně oddělená:
   - vpravo je stále vidět Layout panel/tab i když by single-select měl zobrazovat jen single-node panely
   - v multi-select módu chci pouze jeden Layout panel přes celou šířku pravé strany
   - v single-select módu chci pouze 4 panely / taby:
     - Edit
     - Add node
     - Delete
     - Import
   - Layout v single-select módu nechci
4. Home button stále neodkazuje na správný root.
   Aktuálně míří na `Facility`, ale chci aby mířil na první skutečný top parent / external root.
   V aktuálním datasetu je správný target `external_grid`.
   Implementace ale má být obecná, ne hardcoded string hack, pokud to jde.
5. Když vlezu do editoru, viewport je extrémně zoomed out.
   Chci, aby vstup do editor mode začínal stejně, jako kdybych kliknul na správně fungující Home.
6. Grid je už vizuálně nekonečný / velký, ale node do něj stále nejdou reálně posouvat.
   Chování naznačuje, že někde stále existuje skrytý movement / placement clamp nebo floor/canvas bound.
   Chci odstranit tuto umělou hranici tak, aby node šly posouvat i mimo původní omezenou oblast.
7. V multi-selectu mají být disabled pouze nejednoznačné single-node akce.
   V praxi:
   - `Add node` musí být při multi-selectu disabled
   - ostatní single-node specifické akce musí respektovat single-vs-multi logiku
   - globální akce mají zůstat aktivní

## Expected behavior
- Multi-select a single-select jsou vzájemně výlučné
- Rectangle selection vybírá pouze skutečně zasažené uzly
- Žádný unrelated node mimo selected set není vizuálně označen
- Když je selected count == 1:
  - zobrazují se jen panely/taby:
    - Edit
    - Add node
    - Delete
    - Import
  - Layout panel/tab se nezobrazuje
- Když je selected count > 1:
  - single-node focus je null / inactive
  - zobrazí se jen Layout panel přes celou pravou stranu
  - single-node panely/taby jsou schované
- Group drag:
  - box-select více node
  - chytnu jeden z selected node
  - pohne se celá skupina
  - relativní pozice se zachovají
  - finální pozice snapnou na grid
- Align / Distribute fungují jen pro:
  - horizontal center
  - vertical center
  - horizontal distribute
  - vertical distribute
  a všechny výsledné pozice snapují na grid
- Home míří na skutečný top root / external root
- Vstup do editor módu inicializuje viewport stejně jako Home
- Grid / canvas už uměle neomezuje pohyb node

## Constraints
- Keep solution general
- Do not introduce building-specific hacks
- Do not break imported dataset / analytics
- Reuse existing save / dirty / undo logic
- Prefer minimal robust change
- First fix selection state model, then group drag
- Fix behavior, not just visual styling

## Acceptance criteria
- unrelated node outside the rectangle selection is no longer highlighted
- single-select and multi-select are mutually exclusive
- single-select shows only Edit / Add node / Delete / Import
- multi-select shows only Layout panel across full right panel width
- Add node is disabled during multi-select
- group drag works by dragging one selected node
- align/distribute snap to grid
- Home focuses the real external/top root
- entering editor starts at the same logical viewport as Home
- node movement is no longer artificially limited by old grid/canvas bounds

## Notes
Concrete observed issue:
- selecting `pv_roof_branch` + its 3 children also highlights `Electricity`, which should not happen

Concrete UX target:
- single-select => 4 tabs/panels only: Edit / Add node / Delete / Import
- multi-select => 1 Layout panel only, full-width right side
