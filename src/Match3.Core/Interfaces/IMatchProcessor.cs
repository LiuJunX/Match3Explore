using System.Collections.Generic;
using Match3.Core.Structs;

namespace Match3.Core.Interfaces;

public interface IMatchProcessor
{
    int ProcessMatches(ref GameState state, List<MatchGroup> groups);
}
