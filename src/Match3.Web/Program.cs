using Match3.Core.DependencyInjection;
using Match3.Web.Services;
using Match3.Web.Services.AI;

var builder = WebApplication.CreateBuilder(args);

// Data paths from configuration (relative to ContentRootPath)
var scenariosPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["DataPaths:ScenariosPath"] ?? "src/Match3.Core.Tests/Scenarios/Data");
var levelsPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["DataPaths:LevelsPath"] ?? "data/levels");

// Add controllers for API
builder.Services.AddControllers();

// Editor Services (for API)
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
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve WebAssembly files
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Map API controllers
app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
