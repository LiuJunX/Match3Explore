using System;
using Match3.Core.Interfaces;
using Match3.Core.Models.Enums;
using Match3.Core.Models.Grid;
using Match3.Core.Systems.Input;
using Xunit;

namespace Match3.Core.Tests.Systems.Input;

public class StandardInputSystemTests
{
    private readonly StandardInputSystem _inputSystem;
    private const double CellSize = 100.0;

    public StandardInputSystemTests()
    {
        _inputSystem = new StandardInputSystem();
        _inputSystem.Configure(CellSize);
    }

    [Fact]
    public void TapDetected_WhenDistanceIsSmall()
    {
        // Arrange
        bool tapped = false;
        Position? tapPos = null;
        _inputSystem.TapDetected += p => { tapped = true; tapPos = p; };

        // Act
        _inputSystem.OnPointerDown(1, 1, 100, 100);
        // Instant release
        _inputSystem.OnPointerUp(105, 105); // Move 5px (Threshold is 50px)

        // Assert
        Assert.True(tapped, "Tap should be detected");
        Assert.Equal(new Position(1, 1), tapPos);
    }

    [Fact]
    public void SwipeDetected_WhenDistanceIsLargeAndNotDiagonal()
    {
        // Arrange
        bool swiped = false;
        Direction? swipeDir = null;
        _inputSystem.SwipeDetected += (p, d) => { swiped = true; swipeDir = d; };

        // Act
        _inputSystem.OnPointerDown(2, 2, 200, 200);
        _inputSystem.OnPointerUp(300, 200); // Move +100px Right (Threshold 50px)

        // Assert
        Assert.True(swiped, "Swipe should be detected");
        Assert.Equal(Direction.Right, swipeDir);
    }

    [Fact]
    public void Cancelled_WhenDiagonalSwipe_45Degrees()
    {
        // Arrange
        bool swiped = false;
        bool tapped = false;
        _inputSystem.SwipeDetected += (p, d) => swiped = true;
        _inputSystem.TapDetected += p => tapped = true;

        // Act
        // 45 degrees move: +100x, +100y
        _inputSystem.OnPointerDown(0, 0, 0, 0);
        _inputSystem.OnPointerUp(100, 100);

        // Assert
        Assert.False(swiped, "Swipe should be cancelled due to diagonal movement");
        Assert.False(tapped, "Tap should not be detected because distance > threshold");
    }

    [Fact]
    public void Cancelled_WhenDiagonalSwipe_135Degrees()
    {
        // Arrange
        bool swiped = false;
        _inputSystem.SwipeDetected += (p, d) => swiped = true;

        // Act
        // 135 degrees: -x, +y
        _inputSystem.OnPointerDown(100, 0, 100, 0);
        _inputSystem.OnPointerUp(0, 100); // dx = -100, dy = +100

        // Assert
        Assert.False(swiped, "Swipe should be cancelled due to diagonal movement (135 deg)");
    }
}
