I will optimize your "Game Architect" agent by creating a dedicated system prompt file that encapsulates the project's strict architectural guidelines, specifically tailored for your Match-3 engine.

Since I cannot directly modify the internal configuration of your custom agent (as it's likely stored in your local IDE settings or cloud config), I will create a standardized prompt file in your repository that you can use as the source of truth.

### Plan:
1.  **Create Directory**: Create `.trae/prompts/` to store agent personas.
2.  **Create Prompt File**: Create `.trae/prompts/game_architect.md`. This file will define the agent's persona, including:
    *   **Role**: Senior Game Architect specializing in high-performance C# and ECS-lite patterns.
    *   **Strict Constraints**: Enforcing `Match3.Core` independence, zero allocations on hot paths (`Pools.Rent`), and the "System" pattern.
    *   **Workflow**: Mandating a "Design -> Interface -> Implementation -> Test" workflow.
    *   **Templates**: Providing correct code snippets for new Systems (e.g., `I{Name}System` and `{Name}System`).
3.  **Documentation**: I will provide instructions on how to paste this prompt into your agent's configuration.

This approach ensures your agent is perfectly aligned with `project_rules.md` and the actual codebase structure.