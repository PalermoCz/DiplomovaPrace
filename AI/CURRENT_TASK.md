# CURRENT TASK

## Název
Implementační krok 3c: import/editor UX completion + series semantics for cumulative energy counters

## Kontext
Máme hotové:
- built-in typy foundation (`area`, `bus`, `weather`)
- signal taxonomy (`exact signal code` + `signal family`)
- multi-binding foundation
- import write path
- binding persistence
- duplicate guard podle exact signal code
- Signal Analytics panel s active signal selection, trendem a basic stats
- hanging route bug je opravený

Po ručním smoke testu se ukázaly tyto nedodělky:

1. v binding preview chybí viditelná delete akce (červený křížek)
2. parser importu počítá header řádek jako invalid row
3. editor pro `NodeType` nenabízí built-in typy (`area`, `bus`, `weather`)
4. signály `W`, `W_in`, `W_out` se v Signal Analytics chovají jako běžné raw series, ale pro uživatele dávají větší smysl jako cumulative energy counters s odvozeným interval delta pohledem

## Cíl
Dotažení posledních UX/datových nedodělků před dalšími analytickými funkcemi.

---

## Co přesně chci implementovat

### 1. Delete binding UI
Do `NODE BINDINGS` preview přidej viditelnou delete akci.

#### Požadavky
- delete akce musí být skutečně vidět v UI
- delete musí odstranit binding z persistence i registry/read path
- po delete se má refreshnout binding preview i Signal Analytics panel
- po delete musí být možné znovu importovat stejný exact signal code

---

### 2. Header handling v import parseru
Uprav parser fixed CSV importu tak, aby:
- automaticky rozpoznal a přeskočil header řádek typu:
  - `datetime_utc,<valueColumn>`
- header se nesmí počítat jako invalid row

#### Dále
- parser musí umět timestampy jak ve formátu:
  - `2018-01-02T19:15:00.000000+00:00`
- tak i ve formátu:
  - `2017-12-30 23:00:00+00:00`

#### Invalid row reporting
- zachovej validaci invalid řádků
- ale summary musí být srozumitelnější:
  - pokud je header přeskočen, nemá být reportovaný jako chyba

---

### 3. NodeType built-in suggestions
V editoru pro `NodeType` nech volný string input, ale přidej built-in suggestions / dropdown návrhy pro:
- `area`
- `bus`
- `weather`

#### Požadavky
- uživatel pořád musí mít možnost napsat vlastní typ
- nechci z `NodeType` udělat tvrdý enum select
- built-in typy mají být snadno dostupné a viditelné

---

### 4. Series semantics pro `W`, `W_in`, `W_out`
Rozšiř interní signal model o jednoduchou semantics vrstvu:

#### Minimálně
- `sample_series`
- `cumulative_counter`

#### Mapování pro tento krok
- `W`
- `W_in`
- `W_out`
→ `cumulative_counter`

Ostatní aktuálně podporované signály:
→ `sample_series` (pokud není zjevný důvod jinak)

---

### 5. Signal Analytics behavior pro cumulative counters
Uprav Signal Analytics tak, aby pro signály se semantics `cumulative_counter` neukazoval jen raw counter trend jako hlavní default, ale odvozený intervalový pohled.

#### Doporučení pro MVP
- exact signal code zůstává `W`, `W_in`, `W_out`
- ale trend + stats mají defaultně pracovat s **interval delta derived series**
- zároveň přidej malou poznámku / status, že jde o odvozený pohled z cumulative counteru

Pokud je to v tomto kroku moc rozsáhlé pro plné dotažení, tak aspoň:
- interně zaveď semantics vrstvu
- a připrav read path / UI hook na další krok

Ale preferuji, aby už `W` v Signal Analytics dával uživatelsky rozumnější výsledek.

---

## Co je mimo scope tohoto kroku
Neimplementuj:
- near-base
- near-peak
- peak-base ratio
- LDC
- baseline
- EUI
- další KPI
- bulk import script

---

## Pravidla práce
- nejdřív stručně napiš plán
- pak implementuj
- drž se scope tohoto kroku
- po dokončení napiš:
  - co bylo změněno
  - co zůstává na další krok
- proveď build
- aktualizuj `AI/WORKLOG.md`

---

## Akceptační kritéria
Krok je hotový, pokud:
1. `NODE BINDINGS` preview obsahuje viditelnou delete akci
2. delete funguje end-to-end
3. import parser správně přeskočí header
4. parser zvládne oba potvrzené timestamp formáty
5. editor `NodeType` nabízí `area`, `bus`, `weather`, ale pořád dovolí vlastní text
6. `W`, `W_in`, `W_out` mají interně semantics `cumulative_counter`
7. Signal Analytics pro `W*` dává rozumnější výsledek než téměř plochý raw counter trend
8. build projde
