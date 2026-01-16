# ADR-0008: Extract Shared Swap Operations

* **Status**: Accepted
* **Deciders**: Development Team
* **Date**: 2026-01-16

## Context and Problem Statement

Match3Engine and SimulationEngine both implement swap and revert logic independently, leading to feature drift. A bug was discovered where SimulationEngine (used by Web app) lacked the invalid swap revert animation that Match3Engine had. How can we ensure both engines maintain consistent swap behavior?

## Decision Drivers

* **Feature drift prevention**: Both engines must have identical swap/revert logic
* **Single source of truth**: Core game rules should be defined in one place
* **Minimal coupling**: Engines have different runtime requirements (animation vs simulation)
* **Testability**: Shared logic should be independently testable

## Considered Options

* **Option 1**: Merge into a single engine with configuration flags
* **Option 2**: Extract shared logic into a common module with strategy pattern
* **Option 3**: Make SimulationEngine wrap Match3Engine internally

## Decision Outcome

Chosen option: **Option 2 - Extract shared logic with strategy pattern**, because it:
- Keeps each engine optimized for its use case
- Eliminates code duplication without tight coupling
- Allows independent testing of shared logic
- Uses well-understood design patterns

### Positive Consequences

* Swap/revert logic is defined once in `SwapOperations`
* Future rule changes automatically apply to both engines
* Clear separation: `ISwapContext` handles engine-specific behavior
* 10 new unit tests cover the shared logic

### Negative Consequences

* Slight increase in abstraction (one more interface)
* Minor indirection cost (negligible in practice)

## Implementation

### New Components

```
src/Match3.Core/Systems/Swap/
├── ISwapContext.cs           # Strategy interface for engine differences
├── ISwapOperations.cs        # Shared operations interface
├── PendingMoveState.cs       # Unified pending move state
├── SwapOperations.cs         # Shared implementation
├── AnimatedSwapContext.cs    # Match3Engine: animation-based detection
└── InstantSwapContext.cs     # SimulationEngine: timer-based + events
```

### Strategy Pattern

| Behavior | AnimatedSwapContext | InstantSwapContext |
|----------|--------------------|--------------------|
| Position sync | No (AnimationSystem handles) | Yes (immediate) |
| Animation detection | IAnimationSystem.IsVisualAtTarget() | Timer (0.15s) |
| Revert event | None | TilesSwappedEvent(IsRevert=true) |

### Modified Files

* `Match3Engine.cs` - Uses SwapOperations + AnimatedSwapContext
* `SimulationEngine.cs` - Uses SwapOperations + InstantSwapContext

## Validation

* `SwapOperationsTests.cs` - 10 tests covering shared logic
* `SimulationEngineTests.cs` - 20 tests including revert scenarios
* `Match3EngineInteractionTests.cs` - 11 tests for interaction behavior
* All 538 tests pass after refactoring

## Pros and Cons of the Options

### Option 1: Single Engine with Configuration

* Good: Absolute consistency
* Bad: Complexity explosion with flags
* Bad: Performance trade-offs for different use cases

### Option 2: Extract Shared Logic (Chosen)

* Good: Single source of truth for rules
* Good: Each engine stays optimized
* Good: Strategy pattern is well-understood
* Bad: One more abstraction layer

### Option 3: SimulationEngine wraps Match3Engine

* Good: Guarantees feature parity
* Bad: Performance overhead for AI simulation
* Bad: Awkward API for high-speed scenarios
