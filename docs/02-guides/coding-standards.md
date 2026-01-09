# Match3 Coding Standards

## 1. Code Style
*   **Format**: 4 spaces indent, CRLF, Allman braces (start on new line).
*   **Naming**: 
    *   `_camelCase` for private fields.
    *   `PascalCase` for public members/classes.
    *   `IInterface` prefix for interfaces.
*   **Namespaces**: Use file-scoped namespaces (e.g., `namespace Match3.Core;`).

## 2. Critical Rules
1.  **Single Responsibility**: Split classes > 300 lines.
2.  **State Management**: Use explicit State classes where possible.
3.  **CSS Isolation**: Use `.razor.css` files. No `<style>` tags in razor.

## 3. AI Context Guidelines (For AI Agents)
When implementing features or fixing bugs, AI agents must verify:

1.  **Statelessness**: Are you introducing state in a Logic class? (Don't. Put it in GameState).
2.  **Allocation**: Are you creating new objects in a loop? (Don't. Use Pools).
3.  **Dependencies**: Are you referencing `Match3.Web` from `Match3.Core`? (Stop. This is a violation).
4.  **Conventions**:
    *   Use `var` for obvious types.
    *   Prefer `switch` expressions.
    *   All public members in Core need XML docs.

## 4. Testing Strategy
*   **Unit Tests**: Test Logic classes with mocked interfaces.
*   **Architecture Tests**: `Match3.Tests.Architecture` enforces these rules automatically.
