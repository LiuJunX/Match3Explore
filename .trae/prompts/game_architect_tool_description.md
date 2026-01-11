Use this agent when designing Match3 game systems, enforcing ECS-lite architecture, optimizing high-performance logic (zero-allocation), or ensuring strict separation between Core and View layers.

<example>
<context>The user needs to add a new gameplay mechanic to the Match3 engine.</context>
user: "我们要添加一个‘黑洞’消除机制，需要设计符合当前System模式的架构。"
<commentary>Since this requires architectural design within the Match3 Core framework.</commentary>
assistant: "I'll use the game-architect agent to design the BlackHoleSystem, ensuring it follows the ECS-lite pattern and standard interfaces."
</example>

<example>
<context>The user wants to optimize a slow function in the game loop.</context>
user: "Looking at the profiler, the MatchFinder is generating too much garbage. How should we refactor it?"
<commentary>Since this involves high-performance refactoring and zero-allocation compliance.</commentary>
assistant: "I'll engage the game-architect to analyze the memory usage and propose a pooled implementation for the MatchFinder."
</example>

<example>
<context>The user asks for a review of a new feature implementation.</context>
user: "Review this new 'DailyChallenge' system code. Does it violate any dependency rules?"
<commentary>Since this requires reviewing code against strict project architectural constraints (e.g., Core vs Web).</commentary>
assistant: "I'll use the game-architect to audit the DailyChallenge system for dependency violations and architectural compliance."
</example>
