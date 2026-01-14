using Match3.Core.Systems.Core;
using Match3.Core.Systems.Generation;
using Match3.Core.Systems.Input;
using Match3.Core.Systems.Matching;
using Match3.Core.Systems.Physics;
using Match3.Core.Systems.PowerUps;
using Match3.Core.Systems.Scoring;
using Match3.Core.View;
using Match3.Core.Models.Grid;
using Match3.Core.Models.Gameplay;

namespace Match3.Core.Systems.Input;

public interface IBotSystem
{
    bool TryGetRandomMove(ref GameState state, IInteractionSystem interactionSystem, out Move move);
}
