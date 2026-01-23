using Match3.Core.DependencyInjection;
using Match3.Web.Components;
using Match3.Web.Services;
using Match3.Web.Services.AI;

var builder = WebApplication.CreateBuilder(args);

// Data paths from configuration (relative to ContentRootPath)
var scenariosPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["DataPaths:ScenariosPath"] ?? "src/Match3.Core.Tests/Scenarios/Data");
var levelsPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["DataPaths:LevelsPath"] ?? "data/levels");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register game service factory
builder.Services.AddSingleton<IGameServiceFactory>(_ => new GameServiceBuilder().Build());
builder.Services.AddScoped<Match3GameService>();

// Editor Services
builder.Services.AddScoped<Match3.Editor.Interfaces.IPlatformService, Match3.Web.Services.EditorAdapters.WebPlatformService>();
builder.Services.AddScoped<Match3.Editor.Interfaces.IFileSystemService>(sp =>
    new Match3.Web.Services.EditorAdapters.PhysicalFileSystemService(scenariosPath));
builder.Services.AddScoped<Match3.Editor.Interfaces.IJsonService, Match3.Web.Services.EditorAdapters.SystemTextJsonService>();
builder.Services.AddScoped<Match3.Core.Utility.IGameLogger>(sp => new MicrosoftGameLogger(sp.GetRequiredService<ILogger<MicrosoftGameLogger>>()));
builder.Services.AddScoped<Match3.Editor.Logic.GridManipulator>();
builder.Services.AddScoped<Match3.Editor.ViewModels.LevelEditorViewModel>();

builder.Services.AddScoped<ScenarioLibraryService>(sp => new ScenarioLibraryService(scenariosPath));
builder.Services.AddScoped<Match3.Editor.Interfaces.IScenarioService>(sp => sp.GetRequiredService<ScenarioLibraryService>());

builder.Services.AddScoped<LevelLibraryService>(sp => new LevelLibraryService(levelsPath));
builder.Services.AddScoped<Match3.Editor.Interfaces.ILevelService>(sp => sp.GetRequiredService<LevelLibraryService>());

// AI Chat Services
builder.Services.Configure<LLMOptions>(builder.Configuration.GetSection(LLMOptions.SectionName));
builder.Services.AddHttpClient<ILLMClient, OpenAICompatibleClient>();
builder.Services.AddScoped<Match3.Editor.Interfaces.ILevelAIChatService, WebLevelAIChatService>();
builder.Services.AddScoped<Match3.Editor.ViewModels.LevelAIChatViewModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
