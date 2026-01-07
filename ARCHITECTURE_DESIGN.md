# Match3 Architecture & Standards

## 1. Core Principles
*   **Single Responsibility (SRP)**: split classes > **300 lines**. Separate Data, Logic, and UI.
*   **Object-Oriented**: Avoid static "God Classes" (like `GameRules`). Use Interfaces/Polymorphism.
*   **Dependency Inversion**: `UI` -> `ViewModel` -> `Core`. Core never depends on Web.

## 2. Architecture Layers
*   **Match3.Web (UI)**: Blazor components, View-only. No game rules.
*   **Match3.Core (Domain)**:
    *   `Models`: Pure data (`Tile`, `Grid`).
    *   `Interfaces`: Abstract behaviors (`IMatchFinder`).
    *   `Services`: Logic implementations.
    *   `States`: Game state machine.

## 3. Critical Coding Standards

### Code Style & Naming
*   **Format**: 4 spaces indent, CRLF, Allman braces (start on new line).
*   **Naming**: 
    *   `_camelCase` for private fields.
    *   `PascalCase` for public members/classes.
    *   `IInterface` prefix for interfaces.
*   **Namespaces**: Use file-scoped namespaces (e.g., `namespace Match3.Core;`).

### Best Practices
*   **Randomness**: MUST use `Match3.Core.IRandom` interface. NEVER use `System.Random`/`Guid` directly (breaks determinism).
*   **Performance**: Pass `GameState` by `ref` or `in` to avoid struct copying.
*   **State Management**: Use explicit State classes (`IdleState`), avoid boolean flags in Controller.

### Web UI (Blazor)
*   **CSS Isolation**: MUST use `.razor.css` files. No `<style>` tags in razor.
*   **MVVM**: Move C# logic to `ViewModel` or `Code-behind` (`.razor.cs`).
*   **Componentization**: Break large pages into smaller components.
