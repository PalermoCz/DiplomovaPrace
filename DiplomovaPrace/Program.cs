using DiplomovaPrace.Components;
using DiplomovaPrace.Persistence;
using DiplomovaPrace.Services;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// ── Lokální konfigurace (pouze lokální, není v gitu) ──────────────────────
// appsettings.Local.json může definovat: DatabasePath, Facility:NodesCsvPath,
// Facility:EdgesCsvPath, Facility:ForceMigration
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Razor Pages for auth (server-rendered, not interactive)
builder.Services.AddRazorPages();

// Registrace Radzen komponent
builder.Services.AddRadzenComponents();

// ── Databáze: EF Core + SQLite ─────────────────────────────────────────────
// IDbContextFactory<AppDbContext> (Singleton) — bezpečné pro use ze Singleton služeb.
// Každá operace si vytvoří vlastní krátkožijící DbContext přes factory.CreateDbContext().
// DatabasePath: z appsettings.Local.json (D:\DataSet\metering.db) nebo fallback ContentRoot.
var dbPath = builder.Configuration["DatabasePath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "metering.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ── Persistence vrstva ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IMeasurementRepository, EfMeasurementRepository>();

builder.Services.AddScoped<ICsvMeasurementImportService, CsvMeasurementImportService>();
builder.Services.AddSingleton<FacilityEditorStateService>();
builder.Services.AddScoped<FacilityNodeSeriesImportService>();
builder.Services.AddScoped<FacilityImportService>();
builder.Services.AddScoped<FacilityQueryService>();
// FacilityDataBindingRegistry: Singleton — načte dataset_bindings_fixed.csv jednou při startu.
builder.Services.AddSingleton<FacilityDataBindingRegistry>();
builder.Services.AddSingleton<FacilityWeatherSourceResolver>();
builder.Services.AddScoped<NodeAnalyticsPreviewService>();
builder.Services.AddScoped<FacilityAlertSummaryService>();

// ── Aplikační služby ───────────────────────────────────────────────────────
// Editor služby (zůstávají in-memory — Fáze 2 DB migrace editoru přijde later)
builder.Services.AddSingleton<IBuildingConfigurationService, InMemoryBuildingConfigurationService>();
builder.Services.AddScoped<IEditorSessionService, EditorSessionService>();

// ── Auth-shell v1: Lokální email + password + cookies ─────────────────────
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

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
    // Ensure AppUsers table exists (for auth-shell v1)
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS ""AppUsers"" (
            ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
            ""Email"" TEXT NOT NULL UNIQUE,
            ""PasswordHash"" TEXT NOT NULL,
            ""CreatedAtUtc"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""LastLoginUtc"" TEXT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AppUsers_Email"" ON ""AppUsers"" (""Email"");
    ");
    await AppDbSchemaBootstrap.EnsurePhaseOneRelationshipSchemaAsync(db);
    app.Logger.LogInformation("Databáze inicializována: {Path}", dbPath);

    // ── Force migration: pokud je nastaven flag, smaž stávající facility před seedem ──
    // Po úspěšné migraci nastavit Facility:ForceMigration na "false" v appsettings.Local.json.
    if (app.Configuration["Facility:ForceMigration"] == "true")
    {
        await using var dbMigration = await factory.CreateDbContextAsync();
        var existingFacility = await dbMigration.Facilities
            .FirstOrDefaultAsync(f => f.Name == "Smart Company Facility");
        if (existingFacility != null)
        {
            dbMigration.Facilities.Remove(existingFacility);
            await dbMigration.SaveChangesAsync();
            app.Logger.LogInformation("Force migration: stávající facility odstraněna z DB pro reseed z nových CSV.");
        }
        else
        {
            app.Logger.LogInformation("Force migration: facility v DB nebyla nalezena (fresh DB nebo již provedeno).");
        }
    }

    // ── Seed: facility-centric schematic model (Sprint 2) ──────────────────
    var facilityImporter = scope.ServiceProvider.GetRequiredService<FacilityImportService>();
    var envForFacility = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    await facilityImporter.SeedAsync(envForFacility.ContentRootPath);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

// Map Razor Pages (auth pages: login, register, logout)
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();



