using DiplomovaPrace.Components;
using DiplomovaPrace.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registrace aplikačních služeb
builder.Services.AddSingleton<IBuildingStateService, BuildingStateService>();
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<ISimulationService>(sp => sp.GetRequiredService<SimulationService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SimulationService>());

// Editor služby
builder.Services.AddSingleton<IBuildingConfigurationService, InMemoryBuildingConfigurationService>();
builder.Services.AddScoped<IEditorSessionService, EditorSessionService>();
builder.Services.AddSingleton<IActiveBuildingService, ActiveBuildingService>();
builder.Services.AddSingleton<ExpressionEvaluator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
