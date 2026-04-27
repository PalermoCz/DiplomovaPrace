# CURRENT TASK

Read-only analýza kódbáze FacilityWorkbench.

## Kontext
Aplikace je facility graph editor / analytics workbench pro více budov.
Dryad dataset používáme jen jako referenční realistický dataset pro diplomku a vývoj, ale cílový produkt musí fungovat i bez něj, nad uživatelsky importovanými daty.

### UX a selection model
- aplikace je selection-first
- single focus není pro analytiku source of truth
- klik na node záměrně vybírá subtree / children
- analytika patří do spodní analytics sekce
- weather node je napojený mimo hlavní selection a běžně nebude součástí selection scope, pokud není vybraný root

### Node model
- NodeType zůstává volný string input
- uživatel si může vytvářet vlastní typy
- nechceme zavádět samostatné NodeBehaviorRole
- chceme jen 3 built-in speciální typy:
  - area
  - bus
  - weather
- všechny ostatní typy jsou generic
- area node je strukturální kontejner; typicky sám nemá data, ale child node data má
- weather znamená venkovní meteostanici, ne indoor teploměr

### Data model
- jeden node má do budoucna podporovat více datových bindingů / signálů současně
- nebudeme rozdělovat dataset import a generic import
- import je jen jeden a CSV formát má být jednoduchý:
  - 1. sloupec = timestamp
  - 2. sloupec = hodnota
- nechceme uživatele nutit vybírat:
  - time column
  - value column
  - resolution
  - processing level
- resolution se má řešit interně, nikoliv jako vstup od uživatele
- spacing timestampů může být pravidelný i nepravidelný
- processing level nechceme v UI vůbec
- nechceme derivovat meaning signálu z názvu souboru
- místo toho chceme explicitní volbu signal kind při importu

### Built-in signal kinds pro vestavěné analytiky
Uvažovaný směr:
- exact signal code se volí explicitně při importu
- aplikace má vestavěně rozumět minimálně těmto signal codes:
  - P
  - P1
  - P2
  - P3
  - W
  - W_in
  - W_out
  - U1
  - U2
  - U3
  - I1
  - I2
  - I3
  - PF
  - PF1
  - PF2
  - PF3
  - Q
  - Ta
- další signály mohou být importovány jako custom / advanced
- aplikace má interně umět mapovat exact signal codes na širší signal family:
  - power
  - energy
  - voltage
  - current
  - power_factor
  - reactive_power
  - weather_temperature
  - custom/other

### Diplomka a data quality
- diplomová práce nemá řešit čištění dat
- chceme používat hotová zpracovaná data, zejména `*_corrected_resampled_15min.csv.gz`
- raw/harmonized vrstvy nechceme řešit jako primární vstup

### Legacy
- `weather_main` a podobné legacy special-case klíče nechceme
- NodeTags už nejsou používané a nechceme je vracet
- relevantní metadata jsou:
  - NodeType
  - Zone
  - Meter URN (je potřeba zjistit jeho reálnou roli)

## Cíl
Zjistit, jak do současné architektury realisticky doplnit:
1. built-in special handling pro `area`, `bus`, `weather`
2. multi-signal per-node binding model
3. explicit signal-kind model pro import
4. internal signal-family model pro vestavěné analytiky
5. automatické weather source resolution mimo selection
6. fallback logiku při chybějících built-in signálech
7. cleanup legacy special-case node keys
8. roli Meter URN v novém import a binding modelu

## Co chci zjistit

### 1. Built-in type handling
Najdi nejlepší místo v kódu, kde řešit speciální built-in typy:
- area
- bus
- weather

Chci vědět:
- kde to dnes dává největší smysl zavěsit
- jak to řešit bez zavádění druhého pole typu NodeBehaviorRole
- jaké soubory / služby / komponenty by to ovlivnilo

### 2. Multi-signal per-node binding model
Zjisti:
- kde dnes kód silně předpokládá `1 node = 1 primary binding`
- co by se muselo změnit, aby `1 node = více signals`
- jaký je nejlepší minimální binding model

Navrhni minimální binding strukturu:
- node id
- exact signal code
- unit
- optional source label / meter identifier
- optional internal metadata

Neimplementuj, jen navrhni podle reality kódu.

### 3. Exact signal code vs signal family
Zjisti:
- kde v kódu nejlépe reprezentovat exact signal codes
- kde v kódu nejlépe reprezentovat jejich mapování na signal families
- jak dnes kód rozlišuje power vs non-power signály
- jak by se to propsalo do importu a analytics

### 4. Import model
Zjisti:
- jak nejlépe zapadá do současné architektury model:
  - 1 file = 1 time series
  - 1. sloupec timestamp
  - 2. sloupec value
  - signal kind se vybírá explicitně při importu
  - resolution se nezadává
  - processing level se nezadává
- kde by se to nejlépe zavěsilo do dnešního import workflow
- co by bylo potřeba změnit

### 5. Weather source resolution
Zjisti:
- jak by analytics vrstva měla najít facility weather source, když weather node typicky není v selectionu
- kde to v dnešním kódu nejlépe zavěsit
- jaké legacy assumptions tomu dnes brání

### 6. Fallback logika při chybějících signálech
Na základě reality kódu napiš:
- co by se dnes rozbilo, kdyby chyběl `P`
- co by se rozbilo, kdyby chyběla `W`
- co by se rozbilo, kdyby chyběla `Ta`
- co by se rozbilo, kdyby node měl jen custom signal
- co by bylo potřeba udělat, aby aplikace uměla graceful degradation podle dostupných signals

### 7. Meter URN role
Zjisti:
- jak přesně dnes funguje Meter URN
- jestli je to hlavní identifikátor zdroje
- jestli se přes něj váže binding registry / preview analytika
- jestli má být součástí nového import modelu jako povinné nebo volitelné metadata

### 8. Legacy cleanup
Najdi:
- kde přesně žijí `weather_main` a jiné legacy special-case node keys
- jaké další legacy assumptions existují
- co je potřeba změnit pro čistý nový model

## Output format
1. Executive summary
2. Built-in type handling options
3. Multi-signal feasibility
4. Recommended binding model
5. Exact signal code vs signal family hook points
6. Import hook points
7. Weather source resolution
8. Fallback implications
9. Meter URN findings
10. Legacy cleanup findings
11. Recommended next steps
12. Open questions for human decision

## Pravidla
- read-only
- žádné změny v kódu
- nehádat
- opírat se o konkrétní soubory a symboly
- pokud něco není zřejmé, napsat to explicitně