using Match3.Core.Structs;

namespace Match3.Core.Interfaces;

public interface ITileGenerator
{
    TileType GenerateNonMatchingTile(ref GameState state, int x, int y);
}
