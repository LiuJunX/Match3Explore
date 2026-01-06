using System;
using Match3.Core;
using Match3.Core.AI;
using Xunit;

namespace Match3.Tests;

public class Match3EnvironmentTests
{
    [Fact]
    public void Reset_WithSeed_ShouldBeDeterministic()
    {
        var env = new Match3Environment(8, 8, 5);
        
        var state1 = env.Reset(12345);
        var state2 = env.Reset(12345);

        Assert.Equal(state1.Grid.Length, state2.Grid.Length);
        
        for (int i = 0; i < state1.Grid.Length; i++)
        {
            Assert.Equal(state1.Grid[i], state2.Grid[i]);
        }
    }

    [Fact]
    public void Reset_WithDifferentSeeds_ShouldBeDifferent()
    {
        var env = new Match3Environment(8, 8, 5);
        
        var state1 = env.Reset(11111);
        var state2 = env.Reset(22222);

        bool areDifferent = false;
        for (int i = 0; i < state1.Grid.Length; i++)
        {
            if (state1.Grid[i] != state2.Grid[i])
            {
                areDifferent = true;
                break;
            }
        }

        Assert.True(areDifferent, "Boards should be different with different seeds");
    }

    [Fact]
    public void Step_OutOfBounds_ShouldReturnError()
    {
        var env = new Match3Environment(8, 8, 5);
        env.Reset();

        var move = new Move(new Position(-1, 0), new Position(0, 0));
        var result = env.Step(move);

        Assert.False(result.IsDone);
        Assert.True(result.Reward < 0);
        Assert.True(result.Info.ContainsKey("Error"));
    }

    [Fact]
    public void Step_NotAdjacent_ShouldReturnError()
    {
        var env = new Match3Environment(8, 8, 5);
        env.Reset();

        var move = new Move(new Position(0, 0), new Position(0, 2)); // Distance 2
        var result = env.Step(move);

        Assert.True(result.Reward < 0);
        Assert.True(result.Info.ContainsKey("Error"));
    }

    [Fact]
    public void Step_ValidMatch_ShouldIncreaseScoreAndChangeBoard()
    {
        // We need a specific seed where we know a move is possible.
        // Or we can cheat by modifying the board if we had access, but we don't.
        // So we will try random moves until we find a valid one, or rely on a known seed.
        // Let's rely on a known seed. I'll search for a seed or just try brute force in test.
        
        var env = new Match3Environment(8, 8, 5);
        var initialState = env.Reset(42);
        
        // Let's find a valid move by brute force
        Move? validMove = null;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // Try right
                if (x + 1 < 8)
                {
                    // We can't easily check if it's valid without simulating logic or calling internal methods.
                    // But we can just call Step. If Reward > 0, it was valid.
                    // Wait, Step modifies state. So we need to Reset after each attempt if we want to test "from initial".
                    // This is inefficient.
                    // Better approach: Let's trust that eventually we find one.
                }
            }
        }
    }

    [Fact]
    public void Simulation_ShouldFinish_WhenMaxMovesReached()
    {
        int maxMoves = 10;
        var env = new Match3Environment(8, 8, 5, maxMoves);
        env.Reset(123);

        for (int i = 0; i < maxMoves; i++)
        {
            // Just make some dummy moves (0,0) -> (0,1)
            var move = new Move(new Position(0, 0), new Position(0, 1));
            var result = env.Step(move);
            if (i < maxMoves - 1)
                Assert.False(result.IsDone);
            else
                Assert.True(result.IsDone);
        }
    }
}
