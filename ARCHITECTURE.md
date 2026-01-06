# ThreeMatchTrea Architecture

This document describes the high-level architecture and design of the ThreeMatchTrea project.

## Core Design Philosophy: AI-First & Data-Oriented

The architecture of `Match3.Core` has been strictly refactored to follow a **Data-Oriented Design (DOD)** approach, inspired by ECS (Entity Component System) principles. This is to ensure:
1.  **High Performance**: Minimal memory allocation and cache-friendly data layout.
2.  **Determinism**: Guaranteed reproducibility given a seed.
3.  **AI Readiness**: Easy integration with Reinforcement Learning (RL) and Monte Carlo Tree Search (MCTS) algorithms.

### 1. Data-Logic Separation

We strictly enforce the separation of **State** (Data) and **Logic** (Behavior).

*   **State (`GameState`)**: A pure `struct` containing only data (arrays, integers, scores). It has **zero** logic methods. It is allocated on the stack or as a compact array, making it extremely cheap to clone (Snapshot).
*   **Logic (`GameRules`)**: A `static class` containing pure functions. These functions take `ref GameState` as input and modify it. They are stateless and thread-safe.

**Violation of this principle (e.g., adding logic to GameState or storing state in GameRules) is strictly prohibited.**

### 2. Project Structure

- **Match3.Core**
    - **Structs/**: Contains pure data structures (e.g., `GameState`, `Position`, `TileType`).
    - **Logic/**: Contains pure logic (e.g., `GameRules`).
    - **AI/**: Contains the RL environment wrapper (`Match3Environment`) that exposes the core logic to AI agents.
    - **Interfaces**: `IGameView` (Presentation), `IRandom` (Determinism).

- **Match3.ConsoleDemo**: Reference implementation of the UI layer.
- **Match3.Tests**: Unit tests ensuring correctness of `GameRules` and `Match3Environment`.

## Core Components

### 1. GameState (Struct)
The heart of the system.
- **`TileType[] Grid`**: Flattened 1D array representing the 2D board for better memory locality.
- **`long Score`**: Current game score.
- **`long MoveCount`**: Number of moves made.

### 2. GameRules (Static Logic)
The brain of the system.
- **`Initialize(ref GameState)`**: Fills the board ensuring no initial matches.
- **`ApplyMove(ref GameState, ...)`**: Executes a move, handles matching, gravity, and refilling in a loop until stable.
- **`FindMatches(in GameState)`**: Pure function returning current matches.

### 3. Match3Environment (AI Wrapper)
Implements a standard RL interface (`Reset`, `Step`, `GetState`).
- Wraps `GameState` and `GameRules` into an object-oriented API for easy consumption by external AI frameworks (Python/ML.NET).

### 4. Match3Controller (Legacy/UI Bridge)
A thin wrapper that maintains a `GameState` instance and notifies `IGameView` of changes. It bridges the new Data-Oriented core with the event-driven UI.

## Key Algorithms

### Cascade Resolution
The game loop within `ApplyMove` (and `Match3Controller`) follows this strict sequence:
1.  **Swap**: Tentative swap of tiles.
2.  **Match Check**: If no matches, revert swap.
3.  **Loop**:
    - Find Matches.
    - Clear Tiles.
    - Apply Gravity (Bottom-up scan).
    - Refill (Top-down generation).
    - Repeat until no matches found.

## Future Development Guidelines

1.  **Never add state to Logic classes.**
2.  **Never add logic to State structs.**
3.  **Always use `ref` or `in`** when passing `GameState` to avoid unnecessary copying, unless a snapshot is explicitly needed (e.g., for MCTS branching).
4.  **Randomness**: Always use the `IRandom` interface stored in `GameState`. Never use `System.Random.Shared` or `Guid.NewGuid()`.
