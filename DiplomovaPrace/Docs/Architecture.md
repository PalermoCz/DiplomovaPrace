# Architektura aplikace pro interaktivní vizualizaci budov

## 1. Zdůvodnění volby technologie Blazor Server

Pro implementaci aplikace interaktivní vizualizace budov byl zvolen framework Blazor Server na platformě .NET 10. Tato volba je motivována několika technickými a pragmatickými důvody.

Blazor Server umožňuje real-time komunikaci mezi serverem a klientem prostřednictvím protokolu SignalR, založeného na technologii WebSocket. Veškerá aplikační logika běží na serveru, zatímco klient obsluhuje pouze vykreslování uživatelského rozhraní a zachytávání uživatelských interakcí. Tento model je vhodný pro vizualizační aplikace, kde dochází k průběžným změnám zobrazovaných dat a je vyžadována okamžitá reakce uživatelského rozhraní.

Zásadní výhodou je použití jednotného programovacího jazyka C# jak pro serverovou logiku, tak pro definici uživatelského rozhraní. Razor komponenty umožňují deklarativní popis UI s plnou typovou kontrolou v době kompilace. Odpadá nutnost psát JavaScript pro manipulaci s DOM — framework zajišťuje diferenciální aktualizaci DOM prostřednictvím SignalR spojení.

Pro práci se SVG grafikou je Blazor Server obzvláště vhodný, protože SVG elementy jsou součástí standardu HTML5 a Razor syntax umožňuje přímé datové vazby na SVG atributy (fill, stroke, opacity) bez jakéhokoliv JavaScript interop. Změny SVG atributů jsou přenášeny jako malé řetězcové diffy přes SignalR, což je výkonově efektivní.

Z hlediska nasazení je Blazor Server aplikace standardní ASP.NET Core proces bez nutnosti distribuce klientského kódu. To zjednodušuje deployment a eliminuje problémy s kompatibilitou prohlížečů spojené s WebAssembly.

## 2. Zdůvodnění použití simulovaných dat

Aplikace pracuje výhradně se simulovanými daty namísto reálných dat z IoT infrastruktury. Toto rozhodnutí má několik akademických a praktických důvodů.

Primárním záměrem diplomové práce je návrh a implementace vizualizační vrstvy, nikoliv řešení problematiky sběru a přenosu dat z fyzických senzorů. Použití simulovaných dat umožňuje soustředit se na jádro výzkumné otázky — jak efektivně vizualizovat stav technických systémů budovy v reálném čase.

Simulace je deterministická díky použití pseudonáhodného generátoru s fixním seedem. To zajišťuje reprodukovatelnost výsledků, která je zásadní pro akademickou práci — při opakovaném spuštění aplikace se generuje identická sekvence stavů, což umožňuje konzistentní demonstrace a testování.

Architektura striktně odděluje zdroj dat od prezentační vrstvy prostřednictvím rozhraní `IBuildingStateService`. Simulační engine je zaměnitelný za reálný datový zdroj (MQTT broker, OPC UA server, REST API) pouhým poskytnutím alternativní implementace tohoto rozhraní, bez nutnosti jakýchkoliv změn v komponentách uživatelského rozhraní.

## 3. Architektonická rozhodnutí

### 3.1 Třívrstvá architektura

Aplikace je rozdělena do tří logických vrstev s jednosměrnými závislostmi:

- **Models** — doménový model definovaný jako immutabilní C# records (Building, Floor, Room, Device, DeviceState). Tato vrstva nemá žádné závislosti na ostatních vrstvách.
- **Services** — aplikační logika zahrnující správu stavu (BuildingStateService), simulační engine (SimulationService) a pomocné utility (StateColorMapper). Závisí pouze na vrstvě Models.
- **Components** — Blazor Razor komponenty tvořící uživatelské rozhraní. Závisí na vrstvách Models i Services.

Tato architektura odpovídá principu Separation of Concerns a zajišťuje, že změny v jedné vrstvě minimálně ovlivňují vrstvy ostatní.

### 3.2 Observer pattern

Pro komunikaci mezi simulační vrstvou a uživatelským rozhraním je použit návrhový vzor Observer (GoF). `BuildingStateService` vystupuje jako Subject a vystavuje event `Action? OnStateChanged`. Blazor komponenty se přihlašují k odběru tohoto eventu v metodě `OnInitialized` a odhlašují se v metodě `Dispose`.

Tento přístup byl zvolen místo alternativ (CascadingParameters, Flux/Redux) pro svou jednoduchost a přímou podporu v .NET ekosystému. CascadingParameters by vyžadovaly umělou obalovou komponentu a jsou vázány na hierarchii komponent. Flux pattern (např. knihovna Fluxor) by přidala externí závislost a zbytečnou složitost pro daný rozsah aplikace.

### 3.3 Immutabilní doménový model

Všechny doménové typy jsou implementovány jako C# records. Records poskytují hodnotovou rovnost (value-based equality), immutabilitu by default a koncizní syntax. `DeviceState` je obzvláště vhodný jako record — každý tik simulace vytváří novou instanci namísto mutace existující, což eliminuje celou kategorii chyb souvisejících s konkurencí vláken.

### 3.4 Thread safety

Aplikace řeší přístup z více vláken na dvou úrovních:
- `ConcurrentDictionary<string, DeviceState>` zajišťuje bezpečné souběžné čtení (z Blazor circuitů) a zápis (ze simulačního vlákna).
- `InvokeAsync(StateHasChanged)` v event handlerech komponent zajišťuje marshalling na synchronizační kontext Blazor circuitu.

### 3.5 Dependency Inversion

Komponenty závisí na rozhraní `IBuildingStateService`, nikoliv na konkrétní implementaci `BuildingStateService`. Služby jsou registrovány prostřednictvím .NET dependency injection kontejneru. Tento přístup umožňuje snadnou záměnu implementací (například za mock pro testování nebo za reálný datový zdroj).

## 4. Přínos aplikace

Aplikace demonstruje moderní přístup k vizualizaci technických systémů budov s využitím webových technologií. Hlavní přínosy:

- **Architektonický vzor** přenositelný na reálné BMS (Building Management System) aplikace. Oddělení simulace od vizualizace umožňuje přímou migraci na reálná data.
- **Interaktivní SVG vizualizace** bez závislosti na JavaScript frameworcích. Čistě serverové řešení s nativní podporou SVG v Blazor komponentách.
- **Reaktivní uživatelské rozhraní** reagující na změny stavu v reálném čase prostřednictvím Observer pattern a SignalR.
- **Deterministická simulace** umožňující reprodukovatelné demonstrace a testování vizualizačních scénářů.

## 5. Možnosti budoucího rozšíření

Architektura aplikace je navržena s ohledem na rozšiřitelnost v následujících směrech:

- **Napojení na reálné datové zdroje** — implementace `IBuildingStateService` čerpající data z MQTT brokeru, OPC UA serveru nebo REST API namísto simulace.
- **Historická data a trendy** — rozšíření `BuildingStateService` o buffer historických stavů a vizualizace časových řad prostřednictvím grafové komponenty.
- **Uživatelské role a řízení přístupu** — integrace ASP.NET Core Identity pro autentizaci a autorizaci přístupu k jednotlivým částem budovy.
- **Komplexní SVG půdorysy** — nahrazení obdélníkové geometrie místností za libovolné SVG cesty (path data) pro věrnou reprezentaci skutečných půdorysů.
- **Sofistikovanější simulace** — implementace denních cyklů, korelací mezi zařízeními, scénářů poruch a havarijních stavů.
- **Export dat a reporty** — generování PDF/CSV reportů o stavu budovy a historii alarmů.
