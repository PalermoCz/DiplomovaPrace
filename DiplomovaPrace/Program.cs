using DiplomovaPrace.Components;
using DiplomovaPrace.Persistence;
using DiplomovaPrace.Services;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registrace Radzen komponent
builder.Services.AddRadzenComponents();

// ── Databáze: EF Core + SQLite ─────────────────────────────────────────────
// IDbContextFactory<AppDbContext> (Singleton) — bezpečné pro use ze Singleton služeb.
// Každá operace si vytvoří vlastní krátkožijící DbContext přes factory.CreateDbContext().
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "metering.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── Persistence vrstva ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IMeasurementRepository, EfMeasurementRepository>();

// MeasurementPersistenceService: Singleton BackgroundService s Channel<T>
// Registrujeme jako Singleton i jako IHostedService aby byl dostupný pro DI inject
// (SimulationService ho potřebuje přímo — IHostedService interface nestačí).
builder.Services.AddSingleton<MeasurementPersistenceService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MeasurementPersistenceService>());

builder.Services.AddScoped<ICsvMeasurementImportService, CsvMeasurementImportService>();
builder.Services.AddScoped<IKpiService, KpiService>();
builder.Services.AddScoped<IBaselineService, BaselineService>();
builder.Services.AddScoped<IPortfolioAnalyticsService, PortfolioAnalyticsService>();
builder.Services.AddScoped<Bdg2ImportService>();

// ── Aplikační služby ───────────────────────────────────────────────────────
builder.Services.AddSingleton<IBuildingStateService, BuildingStateService>();
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<ISimulationService>(sp => sp.GetRequiredService<SimulationService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SimulationService>());

// Editor služby (zůstávají in-memory — Fáze 2 DB migrace editoru přijde later)
builder.Services.AddSingleton<IBuildingConfigurationService, InMemoryBuildingConfigurationService>();
builder.Services.AddScoped<IEditorSessionService, EditorSessionService>();
builder.Services.AddSingleton<IActiveBuildingService, ActiveBuildingService>();
builder.Services.AddSingleton<ExpressionEvaluator>();
builder.Services.AddSingleton<IDisplayRuleEvaluator, DisplayRuleEvaluator>();

var app = builder.Build();

// ── Inicializace databáze ──────────────────────────────────────────────────
// EnsureCreated: bezpečné pro vývoj — vytvoří DB a tabulky pokud neexistují.
// POZOR: EnsureCreated je nekompatibilní s Migrate() — nepoužívat oboje zároveň.
// Pro produkci (Fáze 3+) nahradit za: dbContextFactory.CreateDbContext().Database.Migrate()
// a přidat migraci pomocí: dotnet ef migrations add InitialMeasurements
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
    app.Logger.LogInformation("Databáze inicializována: {Path}", dbPath);
    
    // Automatické natažení in-memory konfigurace BDG2 z lokálního metadatového souboru pro persistenci nad restarty
    var bdgService = scope.ServiceProvider.GetRequiredService<Bdg2ImportService>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var metaPath = Path.Combine(env.ContentRootPath, "..", "DataSet", "subset", "metadata_subset.csv");
    var elecPath = Path.Combine(env.ContentRootPath, "..", "DataSet", "subset", "electricity_subset.csv");
    if (File.Exists(metaPath))
    {
        await bdgService.ImportSubsetAsync(metaPath, elecPath, skipMeasurements: true);
        app.Logger.LogInformation("BDG2 konfigurace (in-memory) stabilně rekonstruována na základě subsetu.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapPost("/api/import-bdg2", async (Bdg2ImportService bdgService, IWebHostEnvironment env) =>
{
    var metaPath = Path.Combine(env.ContentRootPath, "..", "DataSet", "subset", "metadata_subset.csv");
    var elecPath = Path.Combine(env.ContentRootPath, "..", "DataSet", "subset", "electricity_subset.csv");
    await bdgService.ImportSubsetAsync(metaPath, elecPath);
    return Results.Ok("Import dokončen.");
});

app.MapGet("/api/test-kpi", async (IKpiService kpiService) =>
{
    var deviceId = "bdg2:Panther_education_Rosalie:em1"; // An actual stable ID we generated
    var from = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var to = new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var query = new DiplomovaPrace.Models.Kpi.KpiQuery(deviceId, from, to);
    var result = await kpiService.CalculateBasicKpiAsync(query);
    return Results.Ok(new { result.RecordCount, result.TotalConsumptionKWh, result.SpecificConsumptionKWhPerM2, result.PeakPowerKW, result.AveragePowerKW });
});

app.MapGet("/api/test-baseline", async (IBaselineService baselineService, IActiveBuildingService activeBuildingService) =>
{
    // bdg2:Panther_education_Rosalie
    activeBuildingService.SetActiveBuilding("bdg2:Panther_education_Rosalie");
    var deviceId = "bdg2:Panther_education_Rosalie:em1";
    // Týden v dubnu 2016
    var from = new DateTime(2016, 12, 1, 0, 0, 0, DateTimeKind.Utc);
    var to = new DateTime(2016, 12, 15, 0, 0, 0, DateTimeKind.Utc);
    var resultNormal = await baselineService.CalculateBaselineSummaryAsync(deviceId, from, to);
    
    // Období bez historie (před daty BDG2)
    var fromNoHistory = new DateTime(2015, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    var toNoHistory = new DateTime(2015, 1, 16, 0, 0, 0, DateTimeKind.Utc);
    var resultNoHistory = await baselineService.CalculateBaselineSummaryAsync(deviceId, fromNoHistory, toNoHistory);

    return Results.Ok(new { Normal = resultNormal, NoHistory = resultNoHistory });
});

app.MapGet("/api/test-portfolio", async (string? fromStr, string? toStr, IPortfolioAnalyticsService portfolioService) =>
{
    var from = string.IsNullOrEmpty(fromStr) ? new DateTime(2016, 5, 1, 0, 0, 0, DateTimeKind.Utc) : DateTime.Parse(fromStr).ToUniversalTime();
    var to = string.IsNullOrEmpty(toStr) ? new DateTime(2016, 5, 30, 0, 0, 0, DateTimeKind.Utc) : DateTime.Parse(toStr).ToUniversalTime();
    var result = await portfolioService.GetPortfolioBenchmarkAsync(from, to);
    return Results.Ok(result);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();



