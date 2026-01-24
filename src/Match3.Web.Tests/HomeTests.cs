using Bunit;
using Xunit;
using Match3.Web.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using Match3.Core;
using Match3.Core.DependencyInjection;
using Match3.Core.Models.Grid;
using Match3.Web.Client.Services;
using Microsoft.AspNetCore.Components.Web;

using System.IO;

namespace Match3.Web.Tests;

public class HomeTests : TestContext, IDisposable
{
    private readonly string _tempDir;

    public HomeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "Match3Tests_Home_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        Services.AddLogging();
        Services.AddSingleton<IGameServiceFactory>(_ => new GameServiceBuilder().Build());
        Services.AddScoped<Match3GameService>();
        // Services.AddScoped<Match3.Editor.Interfaces.IJsonService, Match3.Web.Services.EditorAdapters.SystemTextJsonService>();
        // Services.AddScoped<ScenarioLibraryService>(_ => new ScenarioLibraryService(_tempDir));
    }

    public new void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
        base.Dispose();
    }

    [Fact]
    public void GameBoard_Should_Render_Correctly()
    {
        // Act: Render Home component
        var cut = RenderComponent<Home>();

        // Assert: Verify game board generation (wait for async init)
        cut.WaitForAssertion(() => 
        {
            var board = cut.Find(".board-container");
            Assert.NotNull(board);
        });

        // Verify tiles are rendered (8x8 board, so at least some tiles should exist)
        var tiles = cut.FindAll(".tile");
        Assert.True(tiles.Count > 0, "Should have tiles rendered");
    }
    
    [Fact]
    public void ClickTile_Should_NotCrash()
    {
        // Act
        var cut = RenderComponent<Home>();

        // Wait for board to load
        cut.WaitForAssertion(() => cut.Find(".tile"));

        // Find first tile and click
        var firstTile = cut.Find(".tile");
        var down = new PointerEventArgs { ClientX = 0, ClientY = 0 };
        firstTile.TriggerEvent("onpointerdown", down);
        var board = cut.Find(".board-container");
        var up = new PointerEventArgs { ClientX = 0, ClientY = 0 };
        board.TriggerEvent("onpointerup", up);

        // Manually trigger frame update
        var gameService = Services.GetRequiredService<Match3GameService>();
        gameService.ManualUpdate();

        // Force re-render after state update
        cut.Render();

        // Assert: Board should still render correctly (no crash)
        var tiles = cut.FindAll(".tile");
        Assert.True(tiles.Count > 0, "Should still have tiles after click");
    }
}
