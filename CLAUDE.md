# Workflow Orchestration

## 1. Plan Mode Default
- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately — don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity

## 2. Subagent Strategy
- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

## 3. Self-Improvement Loop
- After ANY correction from the user: update `.claude/tasks/lessons.md` with the pattern
- Write rules for yourself that prevent the same mistake
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start for relevant project

## 4. Verification Before Done
- Never mark a task complete without proving it works
- Diff behavior between main and your changes when relevant
- Ask yourself: "Would a staff engineer approve this?"
- Run tests, check logs, demonstrate correctness

## 5. Demand Elegance (Balanced)
- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes — don't over-engineer
- Challenge your own work before presenting it

## 6. Autonomous Bug Fixing
- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests — then resolve them
- Zero context switching required from the user
- Go fix failing CI tests without being told how

## 7. Prefer Generators Over Hand-Writing

- If a tool exists to generate the artifact, use the tool. Don't hand-write what `dotnet ef migrations add`, `dotnet new`, `ng generate`, `npm init`, schematics, scaffolders, or codegens produce
- Examples: EF migrations + model snapshots (always `dotnet ef`), Angular components/services (`ng g`), new projects (`dotnet new`), OpenAPI clients, protobuf stubs
- Hand-written scaffolding drifts from what the tool expects — downstream commands then refuse to run or emit spurious diffs
- Exception: the generator is unavailable in this environment, or its output must be patched for a known reason — then note *why* in a comment

## 8. Surface Known Design Weaknesses Early

- If you know a planned approach has a weakness (brittle thresholds, weaker signal than an available alternative, known failure mode), **raise it at design time — not after the user discovers it on their own**
- Before implementing critical classification / retrieval / threshold / decision logic: flag 1–2 alternatives you're aware of and ask once: *"recommend adding X — ~15 min, OK?"*
- Cheap improvements (≤15 min, obvious upside): propose inclusion in MVP, not "for later tuning"
- Do not silently implement a specification as-is when you know a materially better variant exists. The user pays for your expertise, not your compliance
- Not valid excuses: "user wants to move fast", "this is tuning not MVP", "I'm following the written plan"
- Test of the rule: after the user finds the weakness themselves, could you have flagged it up-front? If yes — should have raised it

# Task Management

1. **Plan First**: Write plan to `.claude/tasks/todo.md` with checkable items
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step
5. **Document Results**: Add review section to `.claude/tasks/todo.md`
6. **Capture Lessons**: Update `.claude/tasks/lessons.md` after corrections

# Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.
