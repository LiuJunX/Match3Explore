# Match3 Core Patterns & Services

All logic implementation must adhere to these patterns to ensure performance and AI-compatibility.

## 1. Object Pooling (Memory Management)
*   **Why**: To avoid GC spikes during the game loop (60fps).
*   **What**: `MatchGroup`, `TileMove`, `Command` objects must be pooled.
*   **How**:
    *   **Rent**: `Pools.Rent<T>()`
    *   **Return**: `Pools.Return(obj)`
    *   **Forbidden**: `new T()` in hot paths (Update/Process loops).
*   **Utility**: `Match3.Core.Utility`
    *   `GenericObjectPool<T>`: Thread-safe, standard implementation.
    *   Supported Types: `List<T>`, `HashSet<T>`, `Queue<T>`.

**Best Practices**:
1.  **Prefer Pooling**: Use `Pools.ObtainList<T>()` instead of `new` for temporary collections in hot paths.
2.  **Guaranteed Release**: Always use `try...finally` blocks to ensure resources are released back to the pool via `Pools.Release()`.

## 2. Logging
*   **Interface**: `IGameLogger`
*   **Usage**: Inject `IGameLogger` into constructors.
*   **Zero-Allocation**: Use generic overloads `LogInfo<T>(template, arg)` instead of string interpolation `$"..."`.
*   **Forbidden**: `Console.WriteLine`, `Debug.Log`, and `$` string interpolation in hot paths.

## 3. String Handling (Zero-Allocation)
*   **Library**: `ZString` (Cysharp.Text)
*   **Pattern**: Use `ZString.Concat` or `ZString.Format` when you absolutely must manipulate strings in Logic.
*   **Constraint**: Avoid `string` allocations in `Update()`, `ProcessMatches()`, or `ApplyGravity()`.

## 4. Randomness
*   **Interface**: `IRandomService` (from Match3.Random)
*   **Usage**: All RNG must go through this service to ensure determinism for replays/testing.
*   **Forbidden**: `System.Random`.
*   **Rule**: **MUST** use `Match3.Core.Interfaces.IRandom`. NEVER use `System.Random` or `Guid` directly.

## 5. Performance Guidelines
1.  **Single Responsibility**: Split classes > 300 lines.
2.  **Pass by Ref**: Pass `GameState` by `ref` or `in` to avoid struct copying.
3.  **No State in Logic**: Never add state to Logic classes.
4.  **No Logic in State**: Never add logic to State structs.

## 6. Modular Architecture (Mandatory)
To ensure long-term maintainability and AI-collaboration efficiency, all new features must follow the **System-Interface** pattern.

### The Rule of "Systems"
*   **Definition**: A "System" is a stateless logic class that implements a specific `Interface` (e.g., `ScoreSystem : IScoreSystem`).
*   **Responsibility**: Encapsulate a single domain domain (Input, Scoring, Physics, AI).
*   **Integration**:
    *   `Match3Controller` MUST NOT contain business logic. It only coordinates Systems.
    *   All Systems must be injected via constructor (Dependency Injection).
    *   Systems must communicate via method calls or event bus, never by sharing mutable state objects (except `ref GameState`).

### Implementation Checklist
1.  **Define Interface**: Create `I{Feature}System` in `Match3.Core.Interfaces`.
2.  **Implement System**: Create `{Feature}System` in `Match3.Core.Systems`.
3.  **Register**: Add to `Match3Controller` constructor and `Match3GameService`.
4.  **Test**: Create specific unit tests for the System in isolation.
