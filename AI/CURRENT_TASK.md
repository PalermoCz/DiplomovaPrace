# CURRENT TASK

## Název
Implementační krok 11d: finální UX polish Overview + Analysis (selection donut, treemap polish, chart polish, no-scroll hardening)

## Kontext
Máme hotový velký redesign spodní analytics části:
- spodní navigace je `Overview + Analysis`
- Overview má 3 hlavní KPI karty:
  - Net
  - Consumption
  - Production
- Overview má hlavní chart a Top contributors treemap
- Analysis je samostatná workspace
- detail analytics běží nad consumption basis

Po ručním smoke testu je nový směr správný, ale ještě je potřeba doladit několik důležitých UX detailů:

### Co je potřeba opravit / dopolishovat
1. `Baseline overlay` je stále vidět v Overview a má být odstraněn
2. `Selection` panel nahoře je pořád příliš slabý:
   - malý pie chart
   - chybí druhé číslo ve smyslu:
     - `With data`
     - `No data`
3. Selection chart má být větší a graficky silnější:
   - ideálně donut
   - s hlavním číslem uvnitř
4. Treemap funguje, ale potřebuje polish:
   - barvy podle node style
   - stejný hover hintbox jako selection chart
   - lepší práci s textem v malých tiles
   - adaptivní `Other` tak, aby nepřerostlo přes 20 %
   - možnost rozkliknout `Other` a dostat se ke contributorům uvnitř
5. Hlavní chart pořád postrádá dotáhnutý polish a užitečné interakce
6. Na FullHD stále vzniká zbytečný scroll
7. To samé platí i pro `Analysis`

Tento krok je poslední velký UX polish / hardening sprint pro spodní analytics workspace.

---

## Cíl
Dodat finální UX polish tak, aby:
1. Overview působil jako čistý executive summary
2. Selection panel nahoře byl silnější a informativnější
3. treemap byla vizuálně i interakčně dotažená
4. chart byl dotažený jako hlavní vizuální prvek
5. Overview i Analysis se co nejlépe vešly na FullHD / 2K bez zbytečného scrollu

---

## Scope tohoto kroku

### Ano
- odstranit baseline overlay z Overview
- redesign Selection chart na větší donut
- přidat textové second-line statistiky:
  - With data
  - No data
- treemap polish
- chart polish
- `Other` behavior a contributor drill-in
- no-scroll hardening pro Overview i Analysis
- drobné copy / spacing / hierarchy polish

### Ne
- nové KPI
- nové matematické modely
- baseline formula changes
- scatter formula changes
- EUI formula changes
- redesign horního schematic editoru
- nové feature moduly mimo tento polish sprint

---

# Přesný UX kontrakt

## A. Overview

### 1. Baseline overlay pryč
V `Overview` už nechci:
- baseline overlay toggle
- žádný baseline overlay control
- žádný placeholder pro něj

Baseline patří do:
- `Analysis > Baseline`

#### Akceptace
- v Overview nesmí být vidět žádný baseline overlay control

---

### 2. KPI strip
Zachovat 3 rovnocenné karty:
- Net
- Consumption
- Production

#### Požadavky
- všechny 3 stejně vysoké a stejně důležité
- aktivní karta je zvýrazněná
- labely a hodnoty mají být vizuálně silnější než dnes
- subtitle může zůstat krátký, ale klidnější

---

### 3. Main chart
Chart zůstává hlavním prvkem Overview.

#### Co chci doladit
- méně whitespace
- čistší header
- jemnější grid
- lepší kontrast line
- zlepšit legend / pinned state presentation
- lepší vizuální hierarchii mezi aggregate line a contributor overlay line

#### Požadované interakce
Implementuj / dotahej tyto užitečné funkce:

##### a) Double click = reset zoom
Pokud chart runtime umožňuje, přidej:
- double click → reset zoom / reset view

##### b) Export snapshot
Přidej malé, nenápadné tlačítko:
- export chart snapshot
- preferovaně PNG / image export
- pokud je v runtime už dostupná podobná schopnost, využij ji

##### c) Jasnější overlay state
Pokud je contributor:
- hover preview
- nebo pinned

pak to musí být přehledně vidět v chart headeru.

Například:
- `Previewing: H1.Z29`
- `Pinned: H1.Z29`
- `Clear`

Nechci, aby overlay stav působil jako náhodná malá badge bez jasného významu.

##### d) Production chart semantics
V `Production` mode má být production zobrazována jako:
- **kladná velikost výroby**
- ne jako záporná čára

---

## B. Treemap

### 1. Barvy podle node style
Každý treemap tile má používat:
- barvu odvozenou z přiřazeného node style
- stejně jako se pracuje s barvou v horním selection chartu

#### Fallback
Pokud node style barvu nemá:
- použij bezpečný fallback

---

### 2. Hover hintbox
Treemap hover musí používat:
- stejný hintbox / hover pattern jako selection chart nahoře

#### Hover má ukazovat minimálně
- node label
- value
- share %
- semantics mode (`Net / Consumption / Production`)

---

### 3. Adaptivní `Other`
Treemap nesmí fungovat fixně jen jako top N.

#### Pravidlo
- zobraz tolik contributorů, aby:
  - `Other <= 20 %`
- s horním limitem:
  - max 10 contributor tiles + `Other`

#### Důležité
- pokud i při tomto limitu `Other` zůstává velké, je to v pořádku
- ale musí být dobře pojmenované a dobře obsloužené

### 4. `Other` label
Nepoužívej matoucí wording typu:
- `Other / Unclassified`
pokud to jsou ve skutečnosti jen sloučené menší contributory.

#### Doporučené znění
- `Other contributors`

---

### 5. Klik na `Other`
Klik na `Other` musí otevřít malý contributor drill-in.

#### Požadované chování
- otevři malý popover / side list / lightweight panel
- zobraz seznam contributorů uvnitř `Other`
- pro každý:
  - label
  - value
  - share
- hover = preview do chartu
- click = pin do chartu

#### Důležité
Nechci fullscreen modal ani další velký panel.
Má to být lehké a rychlé.

---

### 6. Text rendering v tiles
Text nesmí v malých tiles působit useknutě a rozbitě.

#### Pravidla
- velké tiles:
  - label + value + share
- střední tiles:
  - label + value
- malé tiles:
  - žádný text uvnitř
  - info jen v hoveru

---

## C. Selection panel nahoře

### 1. Pie chart změnit na větší donut
Současný malý pie chart je moc drobný.

#### Chci
- výraznější donut
- vizuálně dominantnější než dnes

### 2. Hlavní číslo dovnitř
Do středu donutu chci:
- hlavní číslo:
  - např. `91`
- pod něj text:
  - `Selected nodes`

To má být hlavní vizuální anchor Selection panelu.

---

### 3. Druhá řada statistik
Vedle / pod donutem chci ještě 2 malé textové statistiky:

- `With data`
- `No data`

#### Důležité
Ne jako chips.
Ne jako velké cardy.
Ne jako technické `Supported / Unsupported`.

#### Chci
malé číslo + malý popisek, např.:

```text
83  With data
8   No data
````

#### Styl

*   decentní
*   ale čitelný
*   musí zapadat do Selection panelu

***

## D. No-scroll hardening

### 1. Overview

Na FullHD (1920×1080) a 2K musí být Overview co nejkompaktnější.

#### Požadavek

*   KPI strip + chart + treemap se mají co nejvíc vejít bez zbytečného vertikálního scrollu
*   pokud je scroll nevyhnutelný, musí být výrazně menší než dnes

### 2. Analysis

Stejný požadavek platí pro Analysis.

#### Požadavek

*   Analysis toolbar + aktivní modul
*   žádný zbytečný vertikální odpad
*   menší card padding
*   menší spacing
*   vždy jen jeden aktivní modul
*   minimalizovat celostránkový scroll

***

## E. Analysis workspace polish

### 1. Toolbar

Toolbar má být kompaktní a čistý.

### 2. Modulové layouty

Každý modul má být kompaktnější:

*   méně vertikálního paddingu
*   méně explanatory textu
*   víc data, méně prose

### 3. Power modul

Udržet:

*   KPI nahoře
*   jeden chart slot
*   `LDC` a `Base vs Peak Over Time` jako switch
*   ne pod sebou

### 4. Baseline modul

Karty + krátký note.
Ne dlouhý textový blok.

### 5. Trend / Scatter / EUI

Stejně:

*   kompaktní, čisté, bez zbytečné výplně

***

# F. Neakceptovatelné výsledky

Tyto výsledky nejsou přijatelné:

*   baseline overlay zůstane v Overview
*   treemap bude mít dál náhodné / nesouvisející barvy
*   `Other` zůstane fixní a nekontrolované přes 20 %
*   nebude možné nahlédnout dovnitř `Other`
*   selection chart zůstane malý pie bez silného vizuálního anchoru
*   `With data / No data` se zobrazí jako chips
*   FullHD / 2K scroll se reálně nezlepší
*   chart overlay state zůstane nečitelný
*   chart nedostane žádný skutečný polish / utility funkce

***

## Pravidla práce

*   nejdřív stručně napiš plán
*   potom implementuj
*   drž se tohoto scope
*   po dokončení napiš:
    *   co bylo změněno v Overview
    *   jak funguje selection donut a `With data / No data`
    *   jak funguje adaptivní `Other`
    *   jak funguje `Other` drill-in
    *   jaké chart utility byly přidány
    *   jak byl vylepšen layout pro FullHD / 2K
*   proveď build
*   aktualizuj `AI/WORKLOG.md`

***

## Akceptační kritéria

Krok je hotový, pokud:

1.  baseline overlay už v Overview není
2.  selection chart je větší donut s číslem uvnitř
3.  v Selection panelu jsou textové statistiky `With data / No data`
4.  treemap používá barvy podle node style
5.  treemap používá stejný hover hintbox pattern jako selection chart
6.  `Other` je adaptivní do cíle max 20 %, s horním limitem contributorů
7.  klik na `Other` umožní zobrazit contributory uvnitř
8.  malé treemap tiles už nepůsobí rozbitě useknutým textem
9.  chart má dotažený overlay state a alespoň 2 utility funkce (např. reset zoom, export)
10. Overview i Analysis jsou kompaktnější a méně scrollují na FullHD / 2K
11. build projde