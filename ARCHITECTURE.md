# ThreeMatchTrea Architecture

This document describes the high-level architecture and design of the ThreeMatchTrea project.

## Project Structure

The solution consists of three main projects:

1.  **Match3.Core**: A .NET Standard 2.1 library containing the core game logic, data structures, and interfaces. It is UI-agnostic.
2.  **Match3.ConsoleDemo**: A .NET Console Application acting as a reference implementation and playable demo. It implements the `IGameView` interface to render the game state to the console.
3.  **Match3.Tests**: An xUnit test project for unit testing the core logic.

## Core Components (Match3.Core)

### 1. GameBoard
The `GameBoard` class is the central data structure. It manages the 2D grid of `TileType` enums.
- **Responsibilities**:
    - Storing the grid state.
    - Match detection (`FindMatches`).
    - Applying gravity (`ApplyGravity`).
    - Refilling the board (`Refill`).
    - Basic grid operations (Get, Set, Swap, InBounds).
- **Initialization**: When created, it fills the board with random tiles, ensuring no initial matches exist (preventing "pre-matched" boards).

### 2. Match3Controller
The `Match3Controller` orchestrates the game flow. It bridges the `GameBoard` and the `IGameView`.
- **Responsibilities**:
    - Validating user moves (swaps).
    - Executing swaps and checking for resulting matches.
    - Managing the "Cascade" loop: Match -> Clear -> Gravity -> Refill -> Repeat.
    - Notifying the view of state changes.

### 3. Interfaces
- **`IGameView`**: Defines how the game communicates with the presentation layer. Methods include `RenderBoard`, `ShowSwap`, `ShowMatches`, etc.
- **`IRandom`**: Abstracts random number generation to allow deterministic testing (e.g., seeding or mocking in tests).

### 4. Data Types
- **`TileType`**: Enum representing the different tile colors (Red, Green, Blue, etc.) and `None` for empty spaces.
- **`Position`**: A simple struct holding (X, Y) coordinates.

## Key Algorithms

### Match Detection
The `FindMatches` method scans the board in two passes:
1.  **Horizontal Scan**: Iterates through each row, counting consecutive identical tiles. If a run of 3 or more is found, the positions are added to a `HashSet`.
2.  **Vertical Scan**: Iterates through each column, similarly counting runs.

This approach ensures that L-shapes and T-shapes are correctly identified as the union of horizontal and vertical matches.

### Gravity
The `ApplyGravity` method processes each column from bottom to top. It maintains a "write pointer" at the bottom-most empty slot. As it scans upwards, any non-empty tile is moved to the write pointer's position, and the write pointer moves up. This effectively "bubbles" the empty slots to the top.

### Cascade Resolution
When a player makes a valid swap, `ResolveCascades` enters a loop:
1.  Find all matches.
2.  If no matches, break the loop.
3.  Clear matched tiles (set to `None`).
4.  Apply gravity.
5.  Refill empty slots with new random tiles.
6.  Update the view.
7.  Repeat from step 1 (to catch chain reactions).