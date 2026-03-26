# Nasazení aplikace do Azure App Service

Tento dokument slouží jako stručný a praktický návod pro publikaci této aplikace (Blazor Server) do služby Azure App Service (např. v rámci programu Azure for Students).

## 1. Bezpečnostní kontrola (Před publikací)
Repozitář byl zkontrolován:
- **Žádná hesla ani tajemství:** Projekt neobsahuje žádné zakódované `ConnectionStringy`, `.env` soubory, API klíče ani přístupové tokeny. Je naprosto bezpečný pro verzování a veřejný deployment.
- **Konfigurace:** Soubory `appsettings.json` obsahují pouze neškodné nastavení logování.

## 2. Příprava publikace (Dotnet Publish)
Pro zkompilování aplikace připravené pro server stačí otevřít terminál ve složce projektu (kde leží `.sln` a složka `DiplomovaPrace`) a spustit publikaci typu **Self-Contained**. Toto nastavení je klíčové, protože aplikace využívá .NET 10 (kterou výchozí Azure instalace zatím stoprocentně nemusí podporovat). Příkaz tak přibalí kompletní běhové prostředí přímo k aplikaci:

```powershell
dotnet publish DiplomovaPrace/DiplomovaPrace.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

Tím se v podsložce `./publish` vygenerují všechny těžší zabalené soubory připravené přímo k nahrání na Windows server.

## 3. Postup v Azure Portálu
Pokud využíváš studentský bezplatný účet (Free F1):

1. **Vytvoření App Service:**
   - Běž do Azure portálu a založ novou službu **Web App**.
   - **Publish:** Code
   - **Runtime stack:** .NET 8 (nebo klidně .NET 9). Protože kompiluješ výše přes parametr as `Self-Contained`, Azure do instalace .NET sáhat nebude a použije tvou přibalenou "desítku" z Windows serveru.
   - **Operating System:** Windows (nezbytné, protože výše publikuješ pro cíl `win-x64`).
   - **Pricing Plan:** Free F1.

2. **Klíčové nastavení pro Blazor Server (Websockety)!**
   Aplikace používá pro živé aktualizace rozhraní SignalR (WebSockets). Na free tieru musíš tuto funkci zapnout ručně:
   - Otevři svou vytvořenou Web App v Azure.
   - V levém menu vyber **Settings -> Configuration**.
   - Nahoře překlikni na záložku **General settings**.
   - Najdi přepínač **Web sockets** a dej ho na **On**.
   - *Ulož změny (Save).*

3. **Nahrání souborů:**
   Nejsnazší nasazení je pomocí rozšíření **Azure App Service** přímo ve VS Code. Stačí:
   - Kliknout pravým na vytvořenou složku `publish` (nebo přímo na balík).
   - Zvolit **"Deploy to Web App..."**.
   - Vybrat svou službu v Azure a počkat na dokončení nahrávání.

Tím máš hotovo a aplikace automaticky naběhne!
