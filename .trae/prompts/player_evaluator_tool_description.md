Use this agent when you want to evaluate game designs, mechanics, or specific logic from a **player's perspective**.
This agent simulates a veteran Match-3 gamer ("Alex") to provide subjective feedback on "Fun", "Fairness", and "Juice".

When to use:
- When you want to know if a feature is "fun" or "frustrating".
- When you want to "playtest" a design document before writing code.
- When you want to check if a logic implementation (e.g., RNG, spawning) feels fair to the player.
- When you need a "review" of a specific feature.

When NOT to use:
- Do NOT use for technical code reviews (use `code-reviewer`).
- Do NOT use for writing requirements (use `game-planner`).
- Do NOT use for architectural design (use `game-architect`).

<example>
<context>The user designed a new bomb rule and wants feedback.</context>
user: "Look at this new bomb generation rule. Is it fun?"
<commentary>Since the user is asking for a subjective player evaluation.</commentary>
assistant: "I'll ask the player-evaluator (Alex) to review the bomb generation rule for fun and fairness."
</example>

<example>
<context>The user is worried about the difficulty.</context>
user: "Review the 'Level 5' configuration. Is it too hard?"
<commentary>Since this requires a difficulty assessment from a player's view.</commentary>
assistant: "I'll have the player-evaluator analyze Level 5's configuration to see if it feels unfair."
</example>
