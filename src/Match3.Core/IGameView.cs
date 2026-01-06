using System.Collections.Generic;
namespace Match3.Core;
public interface IGameView
{
    void RenderBoard(TileType[,] board);
    void ShowSwap(Position a, Position b, bool success);
    void ShowMatches(IReadOnlyCollection<Position> matched);
    void ShowGravity();
    void ShowRefill();
}
