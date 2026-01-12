using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Match3.Core;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Web.Services;

namespace Match3.Web.Components.Game;

public partial class GridBoard : IDisposable
{
    [Inject]
    public Match3GameService GameService { get; set; } = default!;

    // Drag & Drop State is now managed by Match3.Core.InputSystem

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
        // Engine does not expose HandlePointerDown directly anymore.
        // It's handled internally via IInputSystem events, BUT standard input system is NOT directly accessible here easily 
        // unless we expose it or use a different pattern.
        // Wait, Match3Engine has IInputSystem dependency.
        // If we want to feed raw input, we need access to that InputSystem.
        // Or we expose a helper on Engine.
        
        // However, looking at Match3Engine, it has OnTap and OnSwipe but no direct pointer handling exposed.
        // But StandardInputSystem implements IInputSystem.
        
        // Solution:
        // We should expose the InputSystem via Match3GameService so we can feed it events.
        // Or, better, Match3GameService should expose methods to feed input.
        
        GameService.HandlePointerDown(x, y, e.ClientX, e.ClientY);
    }

    private void HandlePointerUp(PointerEventArgs e)
    {
        GameService.HandlePointerUp(e.ClientX, e.ClientY);
    }
// 获取基础图块图标 (当前使用 Emoji)
    private string GetTileBaseIcon(Tile t) 
    {
        // 优先显示炸弹图标（如果存在），不再使用 Overlay 叠加
        if (t.Bomb != BombType.None)
        {
            return t.Bomb switch
            {
                BombType.Horizontal => "↔️",
                BombType.Vertical => "↕️",
                BombType.Ufo => "🛸",
                BombType.Square5x5 => "💣",
                BombType.Color => "🌈",
                _ => ""
            };
        }

        if (t.Type.HasFlag(TileType.Rainbow)) return "🌈";
        
        if (t.Type.HasFlag(TileType.Red)) return "🔴";
        if (t.Type.HasFlag(TileType.Green)) return "🟢";
        if (t.Type.HasFlag(TileType.Blue)) return "🔵";
        if (t.Type.HasFlag(TileType.Yellow)) return "🟡";
        if (t.Type.HasFlag(TileType.Purple)) return "🟣";
        if (t.Type.HasFlag(TileType.Orange)) return "🟠";
        
        return "";
    }

    private bool HasBombOverlay(Tile t)
    {
        // 所有炸弹都已移至 BaseIcon 显示，不再需要 Overlay
        return false;
    }

    private string GetBombOverlayIcon(Tile t)
    {
        return "";
    }
}
