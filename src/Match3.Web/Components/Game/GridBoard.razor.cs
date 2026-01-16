using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Match3.Core;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Presentation;
using Match3.Web.Services;

namespace Match3.Web.Components.Game;

public partial class GridBoard : IDisposable
{
    [Inject]
    public Match3GameService GameService { get; set; } = default!;

    protected override void OnInitialized()
    {
        GameService.OnChange += OnGameStateChanged;
    }

    public void Dispose()
    {
        GameService.OnChange -= OnGameStateChanged;
    }

    private void OnGameStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    private void HandlePointerDown(PointerEventArgs e, int x, int y)
    {
        GameService.HandlePointerDown(x, y, e.ClientX, e.ClientY);
    }

    private void HandlePointerUp(PointerEventArgs e)
    {
        GameService.HandlePointerUp(e.ClientX, e.ClientY);
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == " " || e.Code == "Space")
        {
            GameService.TogglePause();
        }
    }

    /// <summary>
    /// Get the image path for a tile visual.
    /// </summary>
    private string? GetTileImagePath(TileVisual visual)
    {
        const string basePath = "assets/tiles";

        // Bomb types take priority
        if (visual.BombType != BombType.None)
        {
            return visual.BombType switch
            {
                BombType.Horizontal => $"{basePath}/bomb_horizontal.png",
                BombType.Vertical => $"{basePath}/bomb_vertical.png",
                BombType.Ufo => $"{basePath}/bomb_ufo.png",
                BombType.Square5x5 => $"{basePath}/bomb_square_bomb.png",
                BombType.Color => $"{basePath}/bomb_color_bomb.png",
                _ => null
            };
        }

        // Rainbow tile
        if (visual.TileType.HasFlag(TileType.Rainbow)) return $"{basePath}/color_rainbow.png";

        // Regular tile colors
        if (visual.TileType.HasFlag(TileType.Red)) return $"{basePath}/color_red.png";
        if (visual.TileType.HasFlag(TileType.Green)) return $"{basePath}/color_green.png";
        if (visual.TileType.HasFlag(TileType.Blue)) return $"{basePath}/color_blue.png";
        if (visual.TileType.HasFlag(TileType.Yellow)) return $"{basePath}/color_yellow.png";
        if (visual.TileType.HasFlag(TileType.Purple)) return $"{basePath}/color_purple.png";
        if (visual.TileType.HasFlag(TileType.Orange)) return $"{basePath}/color_orange.png";

        return null;
    }
}
