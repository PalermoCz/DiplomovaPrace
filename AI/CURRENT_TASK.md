# CURRENT TASK

## Název
Read-only audit existujících analytických funkcí ve FacilityWorkbench a rozhodnutí keep / rework / remove

## Kontext
Máme schválené:
- `ANALYTICS_THEORY.md`
- `ANALYTICS_IMPLEMENTATION_PLAN.md`

Tyto dokumenty definují cílový analytický model aplikace:
- selection-first
- active exact signal code
- signal family
- MVP scope pro power / energy / weather-aware analytics
- metodickou oporu v literatuře

Zároveň v aplikaci už existují starší nebo rozpracované analytické funkce a UI panely, zejména v tabech:
- Overview
- Breakdown
- Performance
- Compare
- Diagnostics

Požadavek:
Neimplementovat teď další feature naslepo, ale nejdřív zjistit, co už v aplikaci existuje, jak to funguje, a rozhodnout:
- co ponechat
- co použít jako základ
- co předělat
- co odstranit

## Cíl
Udělát read-only audit všech existujících analytics funkcí a porovnat je s cílovým analytickým modelem z teorie a implementačního plánu.

---

## Co přesně chci zjistit

### 1. Inventura existujících analytics funkcí
Projdi kód a zjisti, jaké konkrétní analytics / KPI / panely dnes existují v těchto oblastech:
- Overview
- Breakdown
- Performance
- Compare
- Diagnostics

U každé položky napiš:
- název funkce / panelu
- kde v kódu žije
- co zhruba dělá
- jaké vstupy / signály používá
- jestli je to single-node / aggregate / compare / forecast / baseline / heuristic feature

---

### 2. Metodické posouzení vůči teorii
Porovnej každou funkci s `ANALYTICS_THEORY.md` a `ANALYTICS_IMPLEMENTATION_PLAN.md`.

U každé funkce napiš:
- je metodicky v souladu s cílovým modelem?
- má oporu v naší literatuře / plánovaném MVP?
- nebo je to spíš ad hoc heuristika / legacy experiment / interní utility panel?

---

### 3. Rozhodnutí keep / rework / remove
U každé nalezené funkce rozhodni:
- **KEEP** = má zůstat a dává smysl
- **REWORK** = má smysl, ale metodika / UX / datový model potřebují přepsat
- **REMOVE** = je lepší to odstranit nebo nebrat dál v potaz
- **UNCLEAR** = vyžaduje lidské rozhodnutí

U `REWORK` a `REMOVE` vysvětli proč.

---

### 4. Speciální pozornost věnuj těmto oblastem
Zkontroluj obzvlášť:
- Headline KPI
- Deviation / Baseline detail
- Main Time Series
- Disaggregation / Top Contributors
- Role Breakdown
- Operational Health
- Source Map
- Peak Demand
- Load Factor
- After-hours Load
- Peak Analysis
- Load Duration Curve
- Compare setup / compare chart
- Forecast vs Actual
- Forecast diagnostics

---

### 5. Doporučený další implementační krok
Na konci navrhni:
- co je nejlepší další implementační slice po tomto auditu
- co má být další feature prompt
- co má být případně nejdřív odstraněno / schováno / odloženo

---

## Důležitá pravidla
- read-only
- nic neimplementuj
- neopravuj kód
- jen analyzuj a rozhoduj
- opírej se o konkrétní soubory a symboly
- pokud něco není jasné, napiš to explicitně

---

## Output format
1. Executive summary
2. Inventory of existing analytics features
3. Method alignment vs theory/plan
4. Keep / Rework / Remove matrix
5. Riskiest legacy heuristics
6. Recommended next implementation step
7. Open questions for human decision