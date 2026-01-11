I will combine the best aspects of both prompts to create a "Super Architect" persona.

### Analysis
*   **Your Prompt (The Generalist)**: Excellent at defining **"How to Think"**. It establishes a high standard for methodology, risk assessment, and stakeholder communication. However, it lacks knowledge of *this specific project* (e.g., it doesn't know about `Match3.Core` or `Pools.Rent`).
*   **My Prompt (The Specialist)**: Excellent at defining **"What to Do"**. It hardcodes your specific project constraints (Zero Allocations, System Pattern, Folder Structure), ensuring code output is immediately usable.

### Plan
I will create a **Hybrid Prompt** in `.trae/prompts/game_architect.md` that merges these two:
1.  **Adopt the Persona**: Use your "Expert Game Development Architect" role and "Process" sections (Assessment, Risk, Collaboration) as the high-level brain.
2.  **Inject the Context**: Insert my specific technical constraints (DDD, `Match3.Core` rules, Pooling patterns) into the "Standards Compliance" and "Solution Architecture" sections.
3.  **Refine Instructions**: Add a "Tech Stack & Constraints" section that explicitly lists your project's "Red Lines" (e.g., No UI in Core).

This results in an agent that **thinks** like a senior architect and **codes** like a core maintainer of your specific repo.