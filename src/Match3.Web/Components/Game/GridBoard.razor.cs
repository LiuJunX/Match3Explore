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

    // Drag & Drop State
    private double? _dragStartX;
    private double? _dragStartY;
    private int _dragSourceX = -1;
    private int _dragSourceY = -1;
    private const double DragThreshold = 20.0; // pixels

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
        if (GameService.Engine == null) return;
        
        _dragStartX = e.ClientX;
        _dragStartY = e.ClientY;
        _dragSourceX = x;
        _dragSourceY = y;
    }

    private void HandlePointerUp(PointerEventArgs e)
    {
        if (_dragStartX == null || _dragStartY == null || _dragSourceX == -1) return;

        var deltaX = e.ClientX - _dragStartX.Value;
        var deltaY = e.ClientY - _dragStartY.Value;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        if (distance < DragThreshold)
        {
            OnTileClick(_dragSourceX, _dragSourceY);
        }
        else
        {
            HandleSwipe(deltaX, deltaY);
        }

        // Reset
        _dragStartX = null;
        _dragStartY = null;
        _dragSourceX = -1;
        _dragSourceY = -1;
    }

    private void HandleSwipe(double dx, double dy)
    {
        if (GameService.Engine == null) return;

        Direction direction;
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            direction = dx > 0 ? Direction.Right : Direction.Left;
        }
        else
        {
            direction = dy > 0 ? Direction.Down : Direction.Up;
        }

        var p1 = new Position(_dragSourceX, _dragSourceY);
        GameService.OnSwipe(p1, direction);
    }

    private void OnTileClick(int x, int y)
    {
        GameService.OnTap(x, y);
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
                BombType.Square3x3 => "💣",
                BombType.Color => "🌈",
                _ => ""
            };
        }

        if (t.Type == TileType.Rainbow) return "🌈";
        
        return t.Type switch
        {
            TileType.Red => "🔴",
            TileType.Green => "🟢",
            TileType.Blue => "🔵",
            TileType.Yellow => "🟡",
            TileType.Purple => "🟣",
            TileType.Orange => "🟠",
            _ => ""
        };
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
