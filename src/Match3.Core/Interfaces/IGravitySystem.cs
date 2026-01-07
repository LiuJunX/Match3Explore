using System.Collections.Generic;
using Match3.Core.Structs;

namespace Match3.Core.Interfaces;

public interface IGravitySystem
{
    List<TileMove> ApplyGravity(ref GameState state);
    List<TileMove> Refill(ref GameState state);
}
