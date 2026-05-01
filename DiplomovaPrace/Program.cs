using DiplomovaPrace.Components;
using DiplomovaPrace.Persistence;
using DiplomovaPrace.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// -- Local configuration (local only, not committed) ------------------------
// appsettings.Local.json may define: DatabasePath, Facility:NodesCsvPath,
// Facility:EdgesCsvPath, Facility:ForceMigration
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Razor Pages for auth (server-rendered, not interactive)
builder.Services.AddRazorPages();

// Register Radzen components
builder.Services.AddRadzenComponents();

// -- Database: EF Core + SQLite ---------------------------------------------
// IDbContextFactory<AppDbContext> (Singleton) is safe for singleton services.
// Each operation creates a short-lived DbContext via factory.CreateDbContext().
// DatabasePath: from appsettings.Local.json or ContentRoot fallback.
var dbPath = builder.Configuration["DatabasePath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "metering.db");
var activeFacilityName = builder.Configuration["Facility:ActiveFacilityName"] ?? "Smart Company Facility";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// -- Persistence layer -------------------------------------------------------
builder.Services.AddSingleton<IMeasurementRepository, EfMeasurementRepository>();

builder.Services.AddScoped<ICsvMeasurementImportService, CsvMeasurementImportService>();
builder.Services.AddSingleton<FacilityEditorStateService>();
builder.Services.AddScoped<FacilityNodeSeriesImportService>();
builder.Services.AddScoped<FacilityImportService>();
builder.Services.AddScoped<FacilityQueryService>();
builder.Services.AddScoped<FacilityMembershipService>();
builder.Services.AddScoped<FacilityMembersManagementService>();
// FacilityDataBindingRegistry: singleton loaded once on startup.
builder.Services.AddSingleton<FacilityDataBindingRegistry>();
builder.Services.AddSingleton<FacilityWeatherSourceResolver>();
builder.Services.AddScoped<NodeAnalyticsPreviewService>();
builder.Services.AddScoped<FacilityAlertSummaryService>();

// -- Application services ----------------------------------------------------
// Editor services remain in-memory for now.
builder.Services.AddSingleton<IBuildingConfigurationService, InMemoryBuildingConfigurationService>();
builder.Services.AddScoped<IEditorSessionService, EditorSessionService>();

// -- Auth shell v1: local email + password + cookies ------------------------
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
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

// -- Database initialization -------------------------------------------------
// EnsureCreated is acceptable for development and creates DB/tables when missing.
// Note: EnsureCreated is incompatible with Migrate(); do not use both together.
// For production, switch to Database.Migrate() + explicit EF migrations.
{
    using var scope = app.Services.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
    // Ensure AppUsers table exists (auth-shell v1)
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
    await AppDbSchemaBootstrap.EnsureFacilityMembershipSchemaAsync(db);
    await AppDbSchemaBootstrap.EnsurePhaseOneRelationshipSchemaAsync(db);
    await AppDbSchemaBootstrap.EnsureInviteColumnsAsync(db);
    app.Logger.LogInformation("Database initialized: {Path}", dbPath);

    // -- Force migration: if flag is enabled, remove existing facility before seed --
    // After successful migration, set Facility:ForceMigration to false.
    if (app.Configuration["Facility:ForceMigration"] == "true")
    {
        await using var dbMigration = await factory.CreateDbContextAsync();
        var existingFacility = await dbMigration.Facilities
            .FirstOrDefaultAsync(f => f.Name == "Smart Company Facility");
        if (existingFacility != null)
        {
            dbMigration.Facilities.Remove(existingFacility);
            await dbMigration.SaveChangesAsync();
            app.Logger.LogInformation("Force migration: existing facility removed for reseed.");
        }
        else
        {
            app.Logger.LogInformation("Force migration: facility not found (fresh DB or already completed).");
        }
    }

    // -- Seed: facility-centric schematic model ------------------------------
    var facilityImporter = scope.ServiceProvider.GetRequiredService<FacilityImportService>();
    var envForFacility = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    await facilityImporter.SeedAsync(envForFacility.ContentRootPath);

    if (app.Environment.IsDevelopment())
    {
        var authService = scope.ServiceProvider.GetRequiredService<AuthenticationService>();
        await using var dbDev = await factory.CreateDbContextAsync();
        var normalization = await NormalizeLocalDevelopmentAccountsAsync(dbDev, authService, activeFacilityName);

        app.Logger.LogInformation(
            "Dev account normalization completed. UsersCreated={UsersCreated}, UsersUpdated={UsersUpdated}, MembershipsCreated={MembershipsCreated}, MembershipsUpdated={MembershipsUpdated}, RemovedThrowawayUsers={RemovedThrowawayUsers}",
            normalization.UsersCreated,
            normalization.UsersUpdated,
            normalization.MembershipsCreated,
            normalization.MembershipsUpdated,
            normalization.RemovedThrowawayUsers.Count);

        if (normalization.RemovedThrowawayUsers.Count > 0)
        {
            app.Logger.LogInformation(
                "Dev account normalization removed throwaway users: {Emails}",
                string.Join(", ", normalization.RemovedThrowawayUsers));
        }
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
//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isFacilityWorkbenchRequest = path.Equals("/") || path.StartsWithSegments("/facility");

    if (!isFacilityWorkbenchRequest || context.User?.Identity?.IsAuthenticated != true)
    {
        await next();
        return;
    }

    var appUserIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!int.TryParse(appUserIdClaim, out var appUserId) || appUserId <= 0)
    {
        context.Response.Redirect("/login");
        return;
    }

    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    var membershipService = scope.ServiceProvider.GetRequiredService<FacilityMembershipService>();

    await using var db = await dbFactory.CreateDbContextAsync(context.RequestAborted);
    var activeFacilityId = await db.Facilities
        .Where(f => f.Name == activeFacilityName)
        .Select(f => (int?)f.Id)
        .FirstOrDefaultAsync(context.RequestAborted);

    if (!activeFacilityId.HasValue)
    {
        await next();
        return;
    }

    var membership = await membershipService.ResolveForUserAndFacilityAsync(appUserId, activeFacilityId.Value, context.RequestAborted);
    if (membership is null)
    {
        context.Response.Redirect("/access-denied");
        return;
    }

    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();

// Map Razor Pages (auth pages: login, register, logout)
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task<DevAccountNormalizationResult> NormalizeLocalDevelopmentAccountsAsync(
    AppDbContext db,
    AuthenticationService authService,
    string activeFacilityName,
    CancellationToken ct = default)
{
    var result = new DevAccountNormalizationResult();

    var activeFacilityId = await db.Facilities
        .Where(f => f.Name == activeFacilityName)
        .Select(f => (int?)f.Id)
        .FirstOrDefaultAsync(ct);

    if (!activeFacilityId.HasValue)
    {
        return result;
    }

    var matejUser = await EnsureUserWithPasswordAsync(db, authService, "matej.klibr@tul.cz", "password", result, ct);
    var viewerUser = await EnsureUserWithPasswordAsync(db, authService, "viewer@example.com", "password", result, ct);
    var adminUser = await EnsureUserWithPasswordAsync(db, authService, "admin@example.com", "password", result, ct);

    // Persist new users first so generated IDs exist before membership upserts.
    await db.SaveChangesAsync(ct);

    await EnsureMembershipRoleAsync(db, activeFacilityId.Value, viewerUser.Id, FacilityMembershipRole.Viewer.ToString(), result, ct);
    await EnsureMembershipRoleAsync(db, activeFacilityId.Value, adminUser.Id, FacilityMembershipRole.Admin.ToString(), result, ct);

    await CleanupKnownThrowawayValidationUsersAsync(
        db,
        new[]
        {
            "copilot.topbar.check2@example.com"
        },
        result,
        ct);

    await db.SaveChangesAsync(ct);

    return result;
}

static async Task<AppUserEntity> EnsureUserWithPasswordAsync(
    AppDbContext db,
    AuthenticationService authService,
    string email,
    string password,
    DevAccountNormalizationResult result,
    CancellationToken ct)
{
    var normalizedEmail = email.Trim().ToLowerInvariant();

    var user = await db.AppUsers
        .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

    if (user is null)
    {
        user = new AppUserEntity
        {
            Email = normalizedEmail,
            PasswordHash = authService.HashPassword(password),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.AppUsers.Add(user);
        result.UsersCreated++;
        return user;
    }

    user.Email = normalizedEmail;
    user.PasswordHash = authService.HashPassword(password);
    result.UsersUpdated++;
    return user;
}

static async Task EnsureMembershipRoleAsync(
    AppDbContext db,
    int facilityId,
    int appUserId,
    string role,
    DevAccountNormalizationResult result,
    CancellationToken ct)
{
    var membership = await db.FacilityMemberships
        .FirstOrDefaultAsync(m => m.FacilityId == facilityId && m.AppUserId == appUserId, ct);

    if (membership is null)
    {
        db.FacilityMemberships.Add(new FacilityMembershipEntity
        {
            FacilityId = facilityId,
            AppUserId = appUserId,
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        });
        result.MembershipsCreated++;
        return;
    }

    if (!string.Equals(membership.Role, role, StringComparison.OrdinalIgnoreCase))
    {
        membership.Role = role;
        result.MembershipsUpdated++;
    }
}

static async Task CleanupKnownThrowawayValidationUsersAsync(
    AppDbContext db,
    IEnumerable<string> candidateEmails,
    DevAccountNormalizationResult result,
    CancellationToken ct)
{
    foreach (var email in candidateEmails)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await db.AppUsers
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (user is null)
        {
            continue;
        }

        var hasMembership = await db.FacilityMemberships
            .AnyAsync(m => m.AppUserId == user.Id, ct);

        if (hasMembership)
        {
            continue;
        }

        db.AppUsers.Remove(user);
        result.RemovedThrowawayUsers.Add(normalizedEmail);
    }
}

sealed class DevAccountNormalizationResult
{
    public int UsersCreated { get; set; }
    public int UsersUpdated { get; set; }
    public int MembershipsCreated { get; set; }
    public int MembershipsUpdated { get; set; }
    public List<string> RemovedThrowawayUsers { get; } = new();
}



