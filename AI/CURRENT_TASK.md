# CURRENT TASK

## Název
Implementační krok 11: Aggregate semantics + Overview/Detail redesign

## Kontext
Máme hotové hlavní analytické slices:
- selection-first signal analytics
- trend + basic stats
- subtree aggregation
- power analytics:
  - near-base
  - near-peak
  - peak-base ratio
  - load duration curve
  - on-hour duration
  - after-hours load
  - base vs peak over time
- daily weather-aware baseline
- temperature vs load scatter
- EUI MVP
- mixed-sign aggregate hardening pro scatter a power analytics

Po dosavadním vývoji je hlavní problém už spíš produktový a UX:
- Overview je přehlcený textem a hodnotami
- detailní analytika a hlavní přehled jsou promíchané
- pro aggregate whole-facility / area scope se často pracuje s mixed-sign agregací, což je v hlavním přehledu normální a užitečné, ale pro detailní analytiky metodicky nevhodné
- chceme jasně oddělit:
  - elegantní přehled
  - detailní analytický tool

Teoretický a implementační základ:
- pokud selected scope obsahuje zároveň consumption a production, aplikace může nabídnout pohledy:
  - consumption
  - production
  - net
  a net view je užitečný, ale nemá být jediným hlavním pohledem
- detailní load-shape / weather-aware / EUI analytiky mají být počítány nad metodicky interpretovatelným basis
- starý detailní plán říká, že:
  - Overview má být jednoduchý a hlavní
  - Performance má být kompaktní
  - cílový směr je jeden hlavní chart area a méně scrollu

## Produktové rozhodnutí pro tento krok
### Overview
Overview má pracovat s těmito aggregate semantics pohledy:
- `Net`
- `Consumption`
- `Production`

### Detail analytics / tool
Detailní analytiky mají pracovat **jen nad `Consumption` basis**.

To znamená:
- v detailním toolu nechci přepínač `Net / Consumption / Production`
- net a production jsou přehledové pohledy v Overview
- detailní výpočty (baseline, scatter, power analytics, EUI, signal analytics) se mají řídit consumption-oriented basis

---

## Cíl
Přeorganizovat FacilityWorkbench tak, aby:
1. Overview byl přehledný, kompaktní a elegantní
2. Overview uměl ukázat `Net / Consumption / Production`
3. detailní analytika byla jasně oddělená jako tool vrstva
4. detailní analytiky běžely jen nad `Consumption` basis
5. mixed-sign whole-facility scope přestal být produktově problém v hlavním přehledu

---

## Scope tohoto kroku

### Ano
- aggregate semantics pro Overview:
  - Net
  - Consumption
  - Production
- redesign / přeuspořádání Overview a navazující detailní analytics části
- consumption-only basis pro detailní analytiky
- zmenšení textového a vizuálního chaosu
- lepší oddělení headline overview vs detail tool

### Ne
- nové KPI
- nové matematické metriky
- compare redesign jako samostatný feature slice
- forecast redesign
- další nové analytické funkce mimo přeuspořádání a semantics
- změna schválených vzorců u již implementovaných metrik

---

## Semantický kontrakt

### 1. Overview aggregate semantics
Overview má umět zobrazit tři pohledy nad aggregate scope:

#### a) Net
- čistá signed aggregate bilance
- vhodná pro facility-level přehled

#### b) Consumption
- aggregate spotřebovávaná energie / výkon
- production contribution se sem nemá míchat jako záporná část

#### c) Production
- aggregate výroba
- vhodná pro přehled a kontext

#### Důležité
Tyto tři pohledy jsou **overview semantics**, ne nový detail analytics mode.

---

### 2. Detail analytics semantics
Detailní analytiky se mají počítat **jen nad Consumption basis**.

To platí pro:
- Signal Analytics detail trend / stats
- weather-aware baseline
- temperature vs load scatter
- power analytics:
  - near-base
  - near-peak
  - peak-base ratio
  - LDC
  - on-hour duration
  - after-hours load
  - base vs peak over time
- EUI

#### Důležité
- nepočítej tyto metriky nad raw mixed-sign net aggregate
- neukazuj v detailním toolu přepínač `Net / Consumption / Production`
- detailní tool se má opírat o **consumption-only basis**
- žádné silent fallbacky na jiný nesouvisející signal

---

## Co přesně chci implementovat

### 1. Overview redesign
Přepracuj Overview tak, aby byl výrazně přehlednější.

#### Chci
- kompaktní headline KPI vrstvu
- jeden hlavní chart area
- přehledový semantics switch / přepínač:
  - `Net`
  - `Consumption`
  - `Production`
- méně permanentních textových vysvětlení v hlavním pohledu
- méně “debug-like” detailů v Overview

#### Headline KPI směr
V Overview chci vidět hlavně:
- hlavní energetický headline pro aktuální semantics mode
- kompaktní supporting context
- ne detailní metodické texty

---

### 2. Main chart area
Hlavní chart area v Overview má být jedna hlavní grafická plocha.

#### Chci
- chart reaguje na:
  - selected interval
  - selected scope
  - overview semantics mode (`Net / Consumption / Production`)
- chart area nemá být obklopená zbytečně mnoha textovými bloky

#### Důležité
Nechci v tomto kroku rozkopat chart runtime.
Jde o produktové přeuspořádání a napojení na správnou semantics vrstvu.

---

### 3. Detail analytics / tool separation
Detailní analytiky odděl tak, aby bylo zřejmé:
- tohle je „tool / analysis layer“
- ne hlavní overview dashboard

#### Chci
- zřetelnou sekci pro detailní analytiky
- consumption-only basis pro výpočty
- signal selector a detailní metriky mohou zůstat, ale nemají dominovat hlavnímu přehledu

#### Důležité
- detail analytics nemají běžet nad `Net`
- detail analytics nemají běžet nad `Production`, pokud to není výslovně metodicky schválené
- pro tento krok používáme **Consumption-only**

---

### 4. Existing panel cleanup
Bez zavádění nových funkcí:
- zredukuj textový šum
- schovej nebo zkompaktni nadbytečné explanatory texty
- zachovej důležité unavailable states
- zachovej metodickou korektnost
- ale přestaň zobrazovat vše jako stejně důležité

---

### 5. Minimal semantics explanations
Přidej krátké, ale velmi stručné vysvětlení semantics:

#### Overview
- Net = balance of consumption and production
- Consumption = consumption-only view
- Production = production-only view

#### Detail analytics
- detail calculations use consumption basis

Nechci dlouhé bloky metodického textu v hlavním Overview.

---

## Důležitá pravidla

### 1. Detail analytics = consumption-only
Tohle je klíčové rozhodnutí tohoto kroku.
Neimplementuj přepínač `Net / Consumption / Production` pro detailní analytiky.

### 2. Overview = jediná vrstva pro net/consumption/production switch
Net je přehledová semantics vrstva, ne detailní výpočetní basis.

### 3. Bez nových výpočetních metrik
Nepřidávej nové KPI ani nové výpočty.
Přeorganizuj a přesměruj již existující logiku.

### 4. Bez tichých fallbacků
Kde detailní analytika nemá consumption basis, musí to být stále explicitně unavailable.

### 5. Respektovat stávající selection-first architekturu
Použij:
- current selection scope
- current active signal model
- current aggregate logic
- current binding model

Nestav paralelní nový data world bokem.

---

## Co je mimo scope
Neimplementuj:
- nové KPI
- forecast redesign
- compare redesign jako samostatný feature
- další matematické modely
- change of formulas
- phase auto-summing
- production-specific detail analytics

---

## Pravidla práce
- nejdřív stručně napiš plán
- pak implementuj
- drž se scope tohoto kroku
- po dokončení napiš:
  - co bylo změněno v Overview
  - jak funguje semantics přepínač
  - jak je oddělená detail analytics vrstva
  - jak je zajištěno, že detailní analytiky používají consumption basis
- proveď build
- aktualizuj `AI/WORKLOG.md`

---

## Akceptační kritéria
Krok je hotový, pokud:
1. Overview má přehledový semantics switch `Net / Consumption / Production`
2. hlavní chart area reaguje na tento switch
3. detailní analytiky jsou vizuálně oddělené od hlavního Overview
4. detailní analytiky používají consumption-only basis
5. mixed-sign aggregate whole-facility scope už není problém v hlavním přehledu
6. textový a vizuální chaos je menší než předtím
7. build projde