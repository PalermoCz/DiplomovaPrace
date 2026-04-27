# CURRENT TASK

## Název
Implementační krok 3: signal selection a první obecné analytiky (trend, basic stats, subtree aggregation)

## Kontext
Máme hotové:
- built-in typy `area`, `bus`, `weather`
- signal taxonomy (`exact signal code` + `signal family`)
- multi-binding foundation
- weather source resolver foundation
- minimální import write path
- binding persistence
- read path pro importované bindings
- minimální binding preview na nodu

Aplikace je selection-first a analytics patří do spodní analytics sekce.

---

## Cíl kroku
Implementovat první skutečně použitelnou analytickou vrstvu nad novým binding modelem:

1. umožnit zvolit **aktivní exact signal code** pro current selection scope
2. ukázat první obecné analytiky:
   - trend
   - basic stats
   - subtree aggregation tam, kde dává smysl

Tento krok ještě **NENÍ**:
- baseline
- near-base / near-peak
- LDC
- EUI
- weather-aware analytika
- electrical diagnostics
- formula engine

---

## Už schválené principy
- node může mít více bindingů
- analytics se nesmí řídit jen `primary binding`
- exact signal code je explicitní identita série
- signal family se používá interně
- selection může být:
  - single node
  - subtree
  - multi-node selection
- `area` je strukturální subtree scope
- `bus` je strukturální helper
- `weather` je mimo běžný selection flow
- fixed CSV import už existuje
- fallbacky musí být explicitní, ne tiché

---

## Co přesně chci implementovat

### 1. Active analytics signal selection
Implementuj vrstvu, která pro aktuální selection scope určí, jaké exact signal codes jsou k dispozici.

#### Požadavky
- pro selected node / selection scope zjisti dostupné bindingy
- nabídni uživateli výběr aktivního exact signal code
- pokud je v aktuálním scope právě jeden smysluplný kandidát, může být auto-selected
- pokud jich je více, uživatel si musí vybrat
- pokud žádný není dostupný, ukaž srozumitelný empty state

#### Důležité
- nechci, aby aplikace tiše spadla na libovolný `primary binding`
- nechci, aby se význam signálu odvozoval z názvu souboru
- tato vrstva musí být připravená pro další analytické kroky

---

### 2. Selection-scope signal availability
Implementuj minimální logiku pro zjištění, jaké signály jsou v aktuálním selection scope použitelné.

#### Potřebuji rozlišit alespoň:
- exact signal codes dostupné v aktuálním scope
- signal family
- jestli jde signal:
  - zobrazit jako single-node sérii
  - agregovat přes selection
  - nebo je jen context-only / unsupported pro aggregate

#### Minimální pravidla pro tento krok
- exact signal code se považuje za kompatibilní, pokud je dostupný ve vybraném scope
- subtree aggregation dělej jen pro aditivní signal families:
  - `power`
  - `energy`
- pro ostatní signal families zatím nezkoušej složitou agregaci přes více node
- pokud selection není kompatibilní pro aggregation, ukaž to explicitně

---

### 3. Trend analytika
Implementuj první obecnou analytiku:
- trend selected signalu v čase

#### Požadavky
- funguje pro single node
- funguje pro subtree / multi-node selection jen když je aggregation validní
- používá nový active signal selection
- používá nový multi-binding read path
- musí běžet ve spodní analytics sekci

#### Scope
- žádná pokročilá stylizace
- žádná baseline overlay
- jen první čistý trend chart

---

### 4. Basic stats
Přidej k trendu minimální základní statistiky pro aktuální signal scope:

#### Minimálně
- min
- max
- average
- count of points
- time range / coverage pokud to je v tomto kroku snadné

#### Důležité
- stats se musí počítat nad skutečně použitou sérií / agregací
- ne nad jiným fallback signálem

---

### 5. Subtree aggregation – první verze
Implementuj první bezpečnou verzi subtree aggregation.

#### Požadavky
- funguje pro `area` a jiné výběry subtree
- pouze pro aditivní families:
  - `power`
  - `energy`
- agregace = součet hodnot v čase přes kompatibilní nody
- pokud selection obsahuje nekompatibilní mix, ukaž explicitní unavailable / incompatible state

#### Důležité
- v tomto kroku neimplementuj složité phase-summing heuristiky
- nepokoušej se ještě sčítat `P1+P2+P3` do `P` automaticky
- to necháme až na další rozhodnutí / krok

---

### 6. Minimal UI integration
Pokud je to možné v tomto kroku, doplň do analytics sekce:
- dropdown / selector aktivního exact signal code
- trend
- basic stats
- jasné empty states / unavailable states

Nechci ještě finální polished UX, ale funkční a čitelné minimum.

---

## Co je mimo scope tohoto kroku
Neimplementuj:
- baseline
- near-base
- near-peak
- peak-base ratio
- load duration curve
- on-hour duration
- EUI
- weather-aware analytics
- electrical diagnostics
- formula engine
- fullscreen analytics

---

## Pravidla práce
- nejdřív stručně napiš plán
- pak implementuj
- drž se scope tohoto kroku
- pokud narazíš na blocker nebo nejasnost, napiš to explicitně
- po dokončení proveď build
- aktualizuj `AI/WORKLOG.md`

---

## Akceptační kritéria
Krok je hotový, pokud platí:

1. U current selection scope lze zjistit dostupné exact signal codes
2. Existuje aktivní analytics signal selection
3. Lze zobrazit trend nad vybraným signálem
4. Lze zobrazit základní stats nad vybraným signálem
5. U subtree selection funguje první bezpečná agregace pro `power` a `energy`
6. Nekompatibilní selection ukazuje srozumitelný unavailable state
7. Build projde
8. Nebyly omylem implementovány KPI mimo scope tohoto kroku
``