# CURRENT TASK

## Název
Implementační krok 2: import write path, binding persistence a minimální import UI pro jednu časovou řadu

## Kontext
Máme hotový foundation krok:
- built-in typy `area`, `bus`, `weather`
- signal taxonomy (`exact signal code` + `signal family`)
- multi-binding read foundation
- facility weather resolver
- první bezpečný cleanup legacy special-case logiky

Teď potřebujeme udělat druhý krok:
- umožnit zapisovat nové signal bindings na node z UI
- bez implementace samotných analytických funkcí

---

## Už schválená pravidla
- aplikace je selection-first
- analytika patří do spodní analytics sekce
- `NodeType` zůstává volný string
- built-in speciální typy jsou jen:
  - `area`
  - `bus`
  - `weather`
- jeden node může mít více datových bindingů
- import je jen jeden, ne dataset/generic split
- CSV formát je fixní:
  - 1. sloupec = timestamp
  - 2. sloupec = hodnota
  - oddělovač = čárka
- nechceme vybírat:
  - time column
  - value column
  - resolution
  - processing level
- resolution má být interní metadata, ne pole v UI
- spacing timestampů může být pravidelný i nepravidelný
- processing level nebude v UI vůbec
- nechceme odvozovat význam signálu z názvu souboru
- uživatel musí explicitně vybrat **exact signal code**
- `Meter URN` má zůstat volitelné metadata zdroje

### Built-in exact signal codes pro import
- `P`
- `P1`
- `P2`
- `P3`
- `W`
- `W_in`
- `W_out`
- `U1`
- `U2`
- `U3`
- `I1`
- `I2`
- `I3`
- `PF`
- `PF1`
- `PF2`
- `PF3`
- `Q`
- `Ta`
- `custom`

---

## Cíl kroku
Implementovat minimální end-to-end import jedné časové řady na vybraný node tak, aby:
1. uživatel mohl na node nahrát 1 soubor = 1 série
2. uživatel zvolil exact signal code
3. uživatel zvolil unit
4. volitelně mohl zadat Meter URN / source label
5. binding se korektně perzistoval
6. šel následně číst přes nový multi-binding foundation model

Tento krok stále **NENÍ** o KPI, baseline ani analytických grafech.

---

## Co přesně chci implementovat

### 1. Minimální import UI
Najdi nejvhodnější místo v existujícím editor/import workflow a přidej minimální UI pro import jedné série.

#### UI má umožnit:
- vybrat cílový node
- vybrat soubor
- vybrat exact signal code z dropdownu
- zadat unit
- volitelně zadat Meter URN / source label

#### UI nesmí obsahovat:
- time column picker
- value column picker
- resolution input
- processing level input
- phase input
- direction input

#### Poznámka
Phase i direction jsou reprezentované přes exact signal code:
- `P1`, `P2`, `P3`
- `W_in`, `W_out`

---

### 2. CSV parsing pro fixed format
Implementuj nebo uprav import tak, aby očekával:
- 1. sloupec = timestamp
- 2. sloupec = value
- oddělovač = čárka

#### Požadavky
- timestamp musí být parsován z prvního sloupce
- value z druhého sloupce
- spacing může být pravidelný i nepravidelný
- import nesmí vyžadovat ruční zadání resolution
- pokud série obsahuje nevalidní řádky, chci bezpečné a srozumitelné chování (validace / chybová zpráva)
- tento krok ještě nemusí řešit složité cleaning scénáře

---

### 3. Binding persistence
Implementuj zápis bindingu na node tak, aby:
- node mohl mít více bindingů
- nový binding byl perzistentní
- binding obsahoval:
  - node id
  - exact signal code
  - unit
  - source reference / file metadata
  - volitelně Meter URN / source label
  - případná interní metadata nutná pro čtení

#### Důležité
- nerozbij stávající graph model
- nepřepiš foundation krok na jiný model
- navazuj na nový multi-binding foundation

---

### 4. Napojení na read path
Po importu musí být binding:
- dohledatelný přes binding registry / nový resolver
- čitelný přes nový multi-binding model
- připravený pro další analytické prompty

Tzn. nechci jen „uložený soubor někde bokem“, ale skutečně napojený binding.

---

### 5. Minimální zobrazení bindingů na nodu
Pokud je to v tomto kroku bezpečné a malé, přidej minimální přehled bindingů na selected node:
- exact signal code
- unit
- případně Meter URN
- případně source file / label

Nemusí to být finální UX, jen minimální ověření, že bindingy jsou na nodu opravdu vidět a použitelné.

Pokud by to scope zbytečně nafouklo, napiš to a nech to na další krok.

---

## Co je mimo scope tohoto kroku
Neimplementuj:
- KPI
- baseline
- trend grafy
- LDC
- EUI
- subtree analytics
- weather-aware výpočty
- automatic energy integration UI
- formula engine
- fullscreen analytics

---

## Pravidla práce
- nejdřív stručně napiš plán
- pak implementuj
- drž se scope tohoto kroku
- neprováděj zbytečné změny mimo tento krok
- pokud narazíš na blocker nebo nejasnost, napiš to explicitně
- po dokončení aktualizuj `AI/WORKLOG.md`
- build musí projít

---

## Akceptační kritéria
Krok je hotový, pokud platí:

1. Existuje minimální UI pro import 1 souboru = 1 série na node
2. Uživatel při importu vybírá exact signal code a unit
3. CSV parser očekává fixed 2-column formát (timestamp, value)
4. Binding se perzistuje na node
5. Node může mít více bindingů
6. Nový binding je dostupný přes read path / registry
7. Build projde
8. Nebyly omylem implementovány analytické funkce mimo scope
``