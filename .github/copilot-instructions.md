# Copilot Instructions pro projekt DiplomovaPrace

## Stručný popis projektu
Projekt je facility-centric aplikace na .NET 10 a Blazor Server pro interaktivní vizualizaci technických systémů budovy, smart metering a analytické přehledy.

## Aktuální produktový směr
- Cílový směr projektu je facility-centric model se schematic-first workspace.
- Hlavní produktový screen je facility-first shell a schematic workspace.
- Preferovaný hlavní pohled aplikace je FacilityWorkbench a související facility-centric UI.
- Legacy building / floor / room přístup už není cílový produktový směr.
- Nevracej projekt zpět k původnímu building-centric UX.
- BDG2 / Panther / Bobcat / Robin není hlavní analytický zdroj finální aplikace.
- Nezaváděj nové fallback mapování na BDG2 nebo jiné cizí datasety jako náhradu za chybějící facility runtime data, pokud to není explicitně zadáno.

## Zdroj pravdy
Při návrhu změn vycházej vždy primárně z aktuálního kódu a současné registrace aplikace:
- `DiplomovaPrace/Program.cs`
- aktuální `.cs` a `.razor` soubory v projektu `DiplomovaPrace`
- `DiplomovaPrace/Docs/Architecture.md`
- `DEPLOYMENT.md`

`CLAUDE.md` používej jen jako doplňkový referenční materiál, pokud je v souladu s aktuálním kódem.
Archivované legacy AI soubory a archivní složky nejsou source of truth.

## Implementační zásady
- Změny dělej minimálně invazivně a pouze v rozsahu požadavku.
- Chraň funkční facility-centric části aplikace.
- Nevracej do projektu legacy AI persona instrukce ani tool-specific identitu.
- Neobnovuj archivovanou Antigravity / Gemini / legacy vrstvu bez explicitního zadání.
- Nevytvářej nové architektonické směry bez explicitního zadání.
- Pokud data nebo runtime source chybí, preferuj poctivý no-data stav před improvizovaným fallbackem.
- Při práci s analytikou a mapováním datasetů preferuj aktuální facility-centric kontrakt a současný stav aplikace.

## Runtime a generated artefakty
Následující typy souborů nejsou source of truth pro návrh architektury ani logiky:
- build a publish výstupy
- dočasné debug logy
- sqlite journal soubory
- generated runtime artefakty

Tyto artefakty ručně neupravuj, pokud to není explicitně požadováno.
Rozhodnutí o změnách vždy opírej o zdrojový kód a projektové dokumenty, ne o runtime artefakty.

## Práce s uživatelskými poznámkami
Soubor `Notes.md` je uživatelský poznámkový prostor.
Neměň ho, nepřesouvej ho a nemaž ho bez explicitního zadání.

## Doporučený pracovní styl pro Copilot
1. Nejdřív přečti jen relevantní soubory pro daný úkol.
2. Navrhni co nejmenší bezpečnou změnu.
3. Proveď změnu bez zásahu do nesouvisejících částí.
4. Ověř build a případně základní funkčnost.
5. Pokud je požadavek nejasný, nejdřív zpřesni kontext ze zdrojových souborů místo domýšlení.
6. Pokud existuje více historických vrstev projektu, preferuj současný facility-centric směr před legacy dokumentací.

## Build a spuštění
- Build: `dotnet build`
- Run: `dotnet run --project DiplomovaPrace`

## Co nedělat bez explicitního zadání
- nevracej archived legacy AI soubory do root kontextu projektu
- neobnovuj BDG2 fallback jako hlavní analytický směr
- nepřepisuj `Notes.md`
- nepřidávej nové technologické vrstvy bez jasného důvodu
- neprováděj široké refaktory mimo požadovaný rozsah