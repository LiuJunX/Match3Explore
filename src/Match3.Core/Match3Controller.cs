using System.Collections.Generic;

namespace Match3.Core;

/// <summary>
/// Orchestrates the game logic, handling moves, matches, and cascading effects.
/// Acts as the bridge between the GameBoard data and the IGameView presentation.
/// </summary>
public sealed class Match3Controller
{
    private readonly GameBoard _board;
    private readonly IGameView _view;

    /// <summary>
    /// Gets the current game board.
    /// </summary>
    public GameBoard Board => _board;

    /// <summary>
    /// Initializes a new instance of the <see cref="Match3Controller"/> class.
    /// </summary>
    /// <param name="board">The game board instance.</param>
    /// <param name="view">The view interface for rendering.</param>
    public Match3Controller(GameBoard board, IGameView view)
    {
        _board = board;
        _view = view;
        _view.RenderBoard(_board.Snapshot());
    }

    /// <summary>
    /// Attempts to swap two tiles. If the swap results in a match, it is processed.
    /// If not, the swap is reverted.
    /// </summary>
    /// <param name="a">Position of the first tile.</param>
    /// <param name="b">Position of the second tile.</param>
    /// <returns>True if the swap resulted in a valid match; otherwise, false.</returns>
    /// <remarks>
    /// <b>Logic Flow:</b>
    /// 1. Validate inputs (bounds and adjacency).
    /// 2. Perform tentative swap.
    /// 3. Check for matches.
    ///    - If match found: Commit swap, trigger cascade, return true.
    ///    - If no match: Revert swap, animate failure, return false.
    /// </remarks>
    public bool TrySwap(Position a, Position b)
    {
        // 1. Validation
        if (!_board.InBounds(a) || !_board.InBounds(b))
        {
            _view.ShowSwap(a, b, false);
            return false;
        }

        // Check adjacency (Manhattan distance must be 1)
        if (System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y) != 1)
        {
            _view.ShowSwap(a, b, false);
            return false;
        }

        // 2. Tentative Swap
        _board.Swap(a, b);

        // 3. Check Outcome
        if (_board.HasAnyMatches())
        {
            // Valid Move
            _view.ShowSwap(a, b, true);
            _view.RenderBoard(_board.Snapshot());
            
            // Process the resulting matches and subsequent falls
            ResolveCascades();
            
            _view.RenderBoard(_board.Snapshot());
            return true;
        }

        // Invalid Move - Revert
        _board.Swap(a, b);
        _view.ShowSwap(a, b, false);
        _view.RenderBoard(_board.Snapshot());
        return false;
    }

    /// <summary>
    /// Continuously clears matches, applies gravity, and refills the board until no more matches exist.
    /// </summary>
    /// <remarks>
    /// <b>Why:</b> A single move can trigger a chain reaction (cascade). We must resolve the state until it stabilizes.
    /// <br/>
    /// <b>How:</b>
    /// Loop forever:
    /// 1. Find matches. If none, break loop (stable state).
    /// 2. Clear matches.
    /// 3. Apply gravity (tiles fall).
    /// 4. Refill empty top spots.
    /// 5. Repeat.
    /// </remarks>
    private void ResolveCascades()
    {
        while (true)
        {
            var matches = _board.FindMatches();
            if (matches.Count == 0) break;

            // 1. Clear Matches
            _view.ShowMatches(matches);
            _board.Clear(matches);
            _view.RenderBoard(_board.Snapshot());

            // 2. Gravity
            _board.ApplyGravity();
            _view.ShowGravity();
            _view.RenderBoard(_board.Snapshot());

            // 3. Refill
            _board.Refill();
            _view.ShowRefill();
            _view.RenderBoard(_board.Snapshot());
        }
    }
}
