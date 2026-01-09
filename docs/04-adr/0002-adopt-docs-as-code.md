# 2. Adopt Docs-as-Code Structure

* **Status**: Accepted
* **Deciders**: Trae AI, User
* **Date**: 2026-01-09

## Context and Problem Statement

As the project grows, the single `ARCHITECTURE.md` file became bloated and hard to maintain. Documentation was scattered between the root directory and various subfolders, making it difficult for both human developers and AI agents to find relevant context. We need a scalable way to manage documentation that evolves with the code.

## Decision Drivers

* **Scalability**: The documentation structure must support adding new modules without cluttering the root.
* **Discoverability**: Information should be categorized logically (Architecture, Guides, API, Business).
* **AI-Friendliness**: Structured markdown files are easier for LLMs to index and retrieve as context.
* **Maintainability**: Documentation should be treated as code (versioned, reviewed).

## Considered Options

* **Option 1**: Keep everything in `README.md` and `ARCHITECTURE.md`.
* **Option 2**: Use an external Wiki (Confluence/Notion).
* **Option 3**: Adopt a structured **Docs-as-Code** approach within the repository.

## Decision Outcome

Chosen option: **Option 3 (Docs-as-Code)**.

### Justification
*   **Code Locality**: Keeping docs next to code ensures they are more likely to be updated.
*   **Version Control**: Docs are versioned with Git, allowing us to see history and blame.
*   **Offline Access**: Developers can read docs without internet access.
*   **Structured Context**: We can point AI agents to specific folders (e.g., `docs/03-api`) for targeted queries.

### Positive Consequences

*   Clear separation of concerns (Architecture vs. Guides vs. Decisions).
*   Introduced **ADR (Architecture Decision Records)** to track *why* changes happen.
*   Root `README.md` becomes a clean entry point.

### Negative Consequences

*   Requires discipline to maintain the directory structure.
*   Existing links in external systems might break (though we kept the repo flat, so internal relative links are fine).

## Implementation Details

*   Created `/docs` directory with subfolders:
    *   `01-architecture`: High-level design and patterns.
    *   `02-guides`: Developer manuals and standards.
    *   `03-api`: Interface specifications (auto-generated in future).
    *   `04-adr`: Decision records.
    *   `05-business`: Game rules and mechanics.
*   Migrated `ARCHITECTURE.md` to `docs/01-architecture/overview.md`.
*   Updated `project_rules.md` to enforce documentation updates.

## Validation

*   **Project Rules**: `project_rules.md` explicitly mandates updating `docs/` when core components change.
*   **CI/CD**: (Future) Verify that new public APIs have corresponding XML documentation.
