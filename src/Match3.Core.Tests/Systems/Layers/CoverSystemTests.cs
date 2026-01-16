using Match3.Core.Events;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Layers;
using Match3.Random;
using Xunit;

namespace Match3.Core.Tests.Systems.Layers;

/// <summary>
/// CoverSystem unit tests.
///
/// Responsibilities:
/// - Managing cover element damage and destruction
/// - Checking if tiles are protected by covers
/// - Syncing dynamic covers with tile movement
/// </summary>
public class CoverSystemTests
{
    private class StubRandom : IRandom
    {
        public float NextFloat() => 0f;
        public int Next(int max) => 0;
        public int Next(int min, int max) => min;
        public void SetState(ulong state) { }
        public ulong GetState() => 0;
    }

    private readonly CoverSystem _coverSystem = new();

    private GameState CreateState(int width = 8, int height = 8)
    {
        var state = new GameState(width, height, 6, new StubRandom());
        // Initialize grid with empty tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                state.SetTile(x, y, new Tile(y * width + x, TileType.Red, x, y));
            }
        }
        return state;
    }

    #region TryDamageCover Tests

    [Fact]
    public void TryDamageCover_NoCover_ReturnsFalse()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(3, 3);
        var events = new BufferedEventCollector();

        // Act
        bool destroyed = _coverSystem.TryDamageCover(ref state, pos, tick: 1, simTime: 0.1f, events);

        // Assert
        Assert.False(destroyed);
        Assert.Empty(events.GetEvents());
    }

    [Fact]
    public void TryDamageCover_SingleHPCover_DestroysCover()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(3, 3);
        state.SetCover(pos, new Cover(CoverType.Cage, health: 1));
        var events = new BufferedEventCollector();

        // Act
        bool destroyed = _coverSystem.TryDamageCover(ref state, pos, tick: 1, simTime: 0.1f, events);

        // Assert
        Assert.True(destroyed);
        Assert.Equal(CoverType.None, state.GetCover(pos).Type);
        Assert.Single(events.GetEvents());
        var evt = Assert.IsType<CoverDestroyedEvent>(events.GetEvents()[0]);
        Assert.Equal(pos, evt.GridPosition);
        Assert.Equal(CoverType.Cage, evt.Type);
    }

    [Fact]
    public void TryDamageCover_MultiHPCover_ReducesHealth()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(2, 2);
        state.SetCover(pos, new Cover(CoverType.IceCover, health: 2));
        var events = new BufferedEventCollector();

        // Act
        bool destroyed = _coverSystem.TryDamageCover(ref state, pos, tick: 1, simTime: 0.1f, events);

        // Assert
        Assert.False(destroyed); // Not destroyed yet
        Assert.Equal(CoverType.IceCover, state.GetCover(pos).Type);
        Assert.Equal(1, state.GetCover(pos).Health);
        Assert.Empty(events.GetEvents()); // No destroy event
    }

    [Fact]
    public void TryDamageCover_MultiHPCover_DestroyedAfterMultipleHits()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(2, 2);
        state.SetCover(pos, new Cover(CoverType.IceCover, health: 2));
        var events = new BufferedEventCollector();

        // Act - First hit
        bool destroyed1 = _coverSystem.TryDamageCover(ref state, pos, tick: 1, simTime: 0.1f, events);
        // Act - Second hit
        bool destroyed2 = _coverSystem.TryDamageCover(ref state, pos, tick: 2, simTime: 0.2f, events);

        // Assert
        Assert.False(destroyed1);
        Assert.True(destroyed2);
        Assert.Equal(CoverType.None, state.GetCover(pos).Type);
        Assert.Single(events.GetEvents());
    }

    [Fact]
    public void TryDamageCover_InvalidPosition_ReturnsFalse()
    {
        // Arrange
        var state = CreateState(8, 8);
        var invalidPos = new Position(-1, -1);
        var events = NullEventCollector.Instance;

        // Act
        bool destroyed = _coverSystem.TryDamageCover(ref state, invalidPos, tick: 1, simTime: 0.1f, events);

        // Assert
        Assert.False(destroyed);
    }

    [Fact]
    public void TryDamageCover_DisabledEvents_NoEventEmitted()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(3, 3);
        state.SetCover(pos, new Cover(CoverType.Cage, health: 1));
        var events = NullEventCollector.Instance;

        // Act
        bool destroyed = _coverSystem.TryDamageCover(ref state, pos, tick: 1, simTime: 0.1f, events);

        // Assert
        Assert.True(destroyed);
        Assert.Equal(CoverType.None, state.GetCover(pos).Type);
        // NullEventCollector doesn't store events, so no exception means success
    }

    #endregion

    #region IsTileProtected Tests

    [Fact]
    public void IsTileProtected_NoCover_ReturnsFalse()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(4, 4);

        // Act
        bool isProtected = _coverSystem.IsTileProtected(in state, pos);

        // Assert
        Assert.False(isProtected);
    }

    [Fact]
    public void IsTileProtected_WithCover_ReturnsTrue()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(4, 4);
        state.SetCover(pos, new Cover(CoverType.Chain, health: 1));

        // Act
        bool isProtected = _coverSystem.IsTileProtected(in state, pos);

        // Assert
        Assert.True(isProtected);
    }

    [Fact]
    public void IsTileProtected_DestroyedCover_ReturnsFalse()
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(4, 4);
        state.SetCover(pos, new Cover(CoverType.Cage, health: 0));

        // Act
        bool isProtected = _coverSystem.IsTileProtected(in state, pos);

        // Assert
        Assert.False(isProtected);
    }

    [Fact]
    public void IsTileProtected_InvalidPosition_ReturnsFalse()
    {
        // Arrange
        var state = CreateState(8, 8);
        var invalidPos = new Position(100, 100);

        // Act
        bool isProtected = _coverSystem.IsTileProtected(in state, invalidPos);

        // Assert
        Assert.False(isProtected);
    }

    [Theory]
    [InlineData(CoverType.Cage)]
    [InlineData(CoverType.Chain)]
    [InlineData(CoverType.Bubble)]
    [InlineData(CoverType.IceCover)]
    public void IsTileProtected_AllCoverTypes_ReturnsTrue(CoverType coverType)
    {
        // Arrange
        var state = CreateState();
        var pos = new Position(3, 3);
        state.SetCover(pos, new Cover(coverType, health: 1));

        // Act
        bool isProtected = _coverSystem.IsTileProtected(in state, pos);

        // Assert
        Assert.True(isProtected);
    }

    #endregion

    #region SyncDynamicCovers Tests

    [Fact]
    public void SyncDynamicCovers_StaticCover_DoesNotMove()
    {
        // Arrange
        var state = CreateState();
        var from = new Position(2, 2);
        var to = new Position(2, 3);
        state.SetCover(from, new Cover(CoverType.Cage, health: 1, isDynamic: false));

        // Act
        _coverSystem.SyncDynamicCovers(ref state, from, to);

        // Assert
        Assert.Equal(CoverType.Cage, state.GetCover(from).Type);
        Assert.Equal(CoverType.None, state.GetCover(to).Type);
    }

    [Fact]
    public void SyncDynamicCovers_DynamicCover_MovesToNewPosition()
    {
        // Arrange
        var state = CreateState();
        var from = new Position(2, 2);
        var to = new Position(2, 3);
        state.SetCover(from, new Cover(CoverType.Bubble, health: 1, isDynamic: true));

        // Act
        _coverSystem.SyncDynamicCovers(ref state, from, to);

        // Assert
        Assert.Equal(CoverType.None, state.GetCover(from).Type);
        Assert.Equal(CoverType.Bubble, state.GetCover(to).Type);
        Assert.True(state.GetCover(to).IsDynamic);
    }

    [Fact]
    public void SyncDynamicCovers_NoCover_NoChange()
    {
        // Arrange
        var state = CreateState();
        var from = new Position(2, 2);
        var to = new Position(2, 3);
        // No cover at 'from' position

        // Act
        _coverSystem.SyncDynamicCovers(ref state, from, to);

        // Assert
        Assert.Equal(CoverType.None, state.GetCover(from).Type);
        Assert.Equal(CoverType.None, state.GetCover(to).Type);
    }

    [Fact]
    public void SyncDynamicCovers_InvalidFromPosition_NoChange()
    {
        // Arrange
        var state = CreateState();
        var from = new Position(-1, -1);
        var to = new Position(2, 3);
        state.SetCover(to, new Cover(CoverType.Chain, health: 1));

        // Act
        _coverSystem.SyncDynamicCovers(ref state, from, to);

        // Assert
        Assert.Equal(CoverType.Chain, state.GetCover(to).Type); // Unchanged
    }

    [Fact]
    public void SyncDynamicCovers_InvalidToPosition_NoChange()
    {
        // Arrange
        var state = CreateState();
        var from = new Position(2, 2);
        var to = new Position(-1, -1);
        state.SetCover(from, new Cover(CoverType.Bubble, health: 1, isDynamic: true));

        // Act
        _coverSystem.SyncDynamicCovers(ref state, from, to);

        // Assert
        Assert.Equal(CoverType.Bubble, state.GetCover(from).Type); // Unchanged
    }

    [Fact]
    public void SyncDynamicCovers_PreservesHealth()
    {
        // Arrange
        var state = CreateState();
        var from = new Position(2, 2);
        var to = new Position(2, 3);
        state.SetCover(from, new Cover(CoverType.Bubble, health: 3, isDynamic: true));

        // Act
        _coverSystem.SyncDynamicCovers(ref state, from, to);

        // Assert
        Assert.Equal(3, state.GetCover(to).Health);
    }

    #endregion

    #region GameState Cover Interaction Tests

    [Theory]
    [InlineData(CoverType.Cage, false)]    // Cage blocks swap
    [InlineData(CoverType.Chain, false)]   // Chain blocks swap
    [InlineData(CoverType.Bubble, false)]  // Bubble blocks swap
    [InlineData(CoverType.IceCover, false)]// IceCover blocks swap
    [InlineData(CoverType.None, true)]     // No cover allows swap
    public void CanInteract_WithCoverType_ReturnsExpected(CoverType coverType, bool expectedCanInteract)
    {
        // Arrange
        var state = CreateState();
        if (coverType != CoverType.None)
        {
            state.SetCover(3, 3, new Cover(coverType, health: 1));
        }

        // Act
        bool canInteract = state.CanInteract(3, 3);

        // Assert
        Assert.Equal(expectedCanInteract, canInteract);
    }

    [Theory]
    [InlineData(CoverType.Cage, false)]    // Cage blocks match
    [InlineData(CoverType.Chain, true)]    // Chain allows match
    [InlineData(CoverType.Bubble, true)]   // Bubble allows match
    [InlineData(CoverType.IceCover, false)]// IceCover blocks match
    [InlineData(CoverType.None, true)]     // No cover allows match
    public void CanMatch_WithCoverType_ReturnsExpected(CoverType coverType, bool expectedCanMatch)
    {
        // Arrange
        var state = CreateState();
        if (coverType != CoverType.None)
        {
            state.SetCover(3, 3, new Cover(coverType, health: 1));
        }

        // Act
        bool canMatch = state.CanMatch(3, 3);

        // Assert
        Assert.Equal(expectedCanMatch, canMatch);
    }

    [Theory]
    [InlineData(CoverType.Cage, false)]    // Cage blocks movement
    [InlineData(CoverType.Chain, false)]   // Chain blocks movement
    [InlineData(CoverType.Bubble, true)]   // Bubble allows movement (dynamic)
    [InlineData(CoverType.IceCover, false)]// IceCover blocks movement
    [InlineData(CoverType.None, true)]     // No cover allows movement
    public void CanMove_WithCoverType_ReturnsExpected(CoverType coverType, bool expectedCanMove)
    {
        // Arrange
        var state = CreateState();
        if (coverType != CoverType.None)
        {
            state.SetCover(3, 3, new Cover(coverType, health: 1));
        }

        // Act
        bool canMove = state.CanMove(3, 3);

        // Assert
        Assert.Equal(expectedCanMove, canMove);
    }

    #endregion
}
