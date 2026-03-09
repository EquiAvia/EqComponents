# EqGraphView v2: Code Review Fixes + Layout Direction + Edge Routing

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix 5 code review issues, add 4-way layout direction support, and add 3 edge routing modes.

**Architecture:** All changes build on the existing EqGraphView component. Layout direction is implemented as a coordinate transform applied after the existing TopToBottom algorithm. Edge routing is a new path-building strategy in the layout engine. Code review fixes are targeted changes to existing files.

**Tech Stack:** Blazor (.NET 10), SVG, xUnit + bUnit

**Model per task:**

| Task | Model | Rationale |
|---|---|---|
| 1. Enums + Options | haiku | Mechanical additions |
| 2. Edge routing paths | sonnet | Algorithm with multiple strategies |
| 3. Direction transform | sonnet | Coordinate math, multiple cases |
| 4. ForestLayout update | sonnet | Must integrate direction + routing |
| 5. Code review fix #1 | haiku | Simple signature change |
| 6. Code review fix #2 | haiku | Remove methods, rename |
| 7. Code review fix #3 | sonnet | Arrow key navigation logic |
| 8. Code review fix #4 | sonnet | CSS custom properties across files |
| 9. Code review fix #5 | haiku | Small perf mode tweaks |
| 10. Component wiring | sonnet | Wire new params, pass to layout |
| 11. Demo page update | sonnet | New UI controls, signature changes |
| 12. Tests | sonnet | New test coverage |

---

## Resuming After Interruption

1. **Check what's already committed:**
   ```bash
   git log --oneline -15
   ```
   Match commit messages to task numbers.

2. **Check for uncommitted partial work:**
   ```bash
   git status
   git diff
   ```

3. **Resume from the next incomplete task.**

4. **Key landmarks:**
   | Task | Key file/change |
   |---|---|
   | 1 | `Enums.cs` has `LayoutDirection` and `EdgeRouting` |
   | 2 | `HierarchicalTreeLayout.cs` has `BuildEdgePath` method |
   | 3 | `HierarchicalTreeLayout.cs` has `TransformForDirection` method |
   | 4 | `ForestLayout.cs` passes direction/routing to delegate |
   | 5 | `EqGraphView.Razor.cs` has `EventCallback<(GraphNode, GraphContextAction)>` |
   | 6 | `EqGraphView.Razor.cs` no longer has `ExpandAll`/`CollapseAll` |
   | 7 | `EqGraphView.Razor.cs` handles ArrowUp/Down/Left/Right |
   | 8 | `.razor.css` files use `var(--eq-*)` |
   | 9 | `EqGraphNode.razor` uses `r="4"` in perf mode |
   | 10 | `EqGraphView.Razor.cs` has `Direction` and `Routing` parameters |
   | 11 | `GraphView.razor` demo has direction/routing dropdowns |
   | 12 | Test files have direction/routing/arrow-key tests |

---

## Phase Structure

- **Phase A** (`graph-v2-phase-a.md`): Layout engine changes (Tasks 1-4)
- **Phase B** (`graph-v2-phase-b.md`): Code review fixes (Tasks 5-9)
- **Phase C** (`graph-v2-phase-c.md`): Wiring, demo, tests (Tasks 10-12)
