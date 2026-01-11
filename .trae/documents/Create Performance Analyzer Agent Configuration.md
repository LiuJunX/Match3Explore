I will create the configuration files for a new "Performance Analyzer" agent in your workspace. This agent will specialize in optimizing C# code, memory management, and algorithm efficiency for the Match3 engine.

### Files to Create

1.  **`performance_analyzer.md`** (System Prompt)
    *   **Role**: Expert Performance Engineer & Profiling Specialist.
    *   **Focus**: Zero-allocation coding, Hot-path optimization, GC pressure reduction, and Algorithmic complexity analysis.
    *   **Project Context**: Enforcing `Match3.Core` rules (e.g., using `Pools.Rent`, `ZString`, avoiding LINQ in `Update`).
    *   **Output Style**: Analysis reports with specific code improvements and benchmark reasoning.

2.  **`performance_analyzer_tool_description.md`** (Tool Definition)
    *   **Purpose**: Defines when I (the Orchestrator) should call this agent.
    *   **Triggers**: Requests about "slowness", "optimization", "memory leaks", "GC spikes", or "profiling".
    *   **Examples**: "Why is the match finding slow?", "Optimize this function for mobile".

### Execution Steps
1.  Create `.trae/prompts/performance_analyzer.md` with the detailed persona and guidelines.
2.  Create `.trae/prompts/performance_analyzer_tool_description.md` with the usage examples.
