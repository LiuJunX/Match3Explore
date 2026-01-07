using System.Collections.Generic;
using Match3.Core.Structs;

namespace Match3.Core.Interfaces;

public interface IMatchFinder
{
    List<MatchGroup> FindMatchGroups(in GameState state, Position? focus = null);
    bool HasMatches(in GameState state);
}
