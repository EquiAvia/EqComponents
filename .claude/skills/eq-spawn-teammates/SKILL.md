---
name: eq-spawn-teammates
description: Use when spawning agent team teammates, assigning roles to subagents, splitting work across parallel agents, or planning team composition for an EqComponents feature.
---

# Agent Teams -- Teammate Roles & Rules

This defines how to split work across Claude Code Agent Team teammates. Each role maps to owned directories so the lead can assign non-overlapping file ownership.

## Teammate Role Table

| Role | Default Model | Owned Projects / Directories | Verification Command | May NOT Touch |
|---|---|---|---|---|
| **Engineer** (default) | Sonnet | `Library/TreeView/`, `Library/wwwroot/`, `equiavia.components.Utilities/` | `dotnet build equiavia.components.sln` | `Client/` (demo app), `Tests/` (unless fixing broken test) |
| **Blazor Component Specialist** | Sonnet | `Library/TreeView/` (Razor markup & component logic), `Library/wwwroot/` (JS interop, CSS) | `dotnet build Library/equiavia.components.Library.csproj` | `equiavia.components.Utilities/`, `Server/`, `Tests/` |
| **Demo App / Integration Specialist** | Sonnet | `Client/`, `Server/`, `Shared/` | `dotnet build equiavia.components.sln` | `Library/TreeView/` (core component), `equiavia.components.Utilities/` |
| **Test Specialist** | Sonnet | `Tests/` | `dotnet test Tests/equiavia.components.Tests.csproj` | All production code (read-only) |
| **Utility Library Specialist** | Sonnet | `equiavia.components.Utilities/` | `dotnet build equiavia.components.Utilities/equiavia.components.Utilities.csproj` | `Library/TreeView/` (reads only), `Client/`, `Server/` |
| **Product Owner / Software Architect** | Opus | `docs/`, `CLAUDE.md` | N/A (review/planning) | All production code (review-only) |
| **Researcher / Explorer** | Haiku | Read-only across entire codebase | N/A (read-only) | All files (write-prohibited) |

**Model selection rationale:** Lead always runs Opus. Opus for roles requiring complex reasoning (Product Owner — architecture). Sonnet for implementation roles (good coding, lower cost). Haiku for read-only/documentation roles (cheapest, sufficient for the task).

## Shared / Conflict-Zone Files

Only one teammate should modify these at a time -- lead must assign explicit ownership per task:

| File / Directory | Typical Contenders | Rule |
|---|---|---|
| `Shared/` | Engineer, Demo App Specialist | Assign to whoever needs the new model; others wait |
| `Library/TreeView/EqTreeView.Razor.cs` | Engineer, Blazor Component Specialist | Core logic — assign to one only |
| `Library/TreeView/EqTreeView.razor` | Engineer, Blazor Component Specialist | Markup — assign to one only |
| `Library/TreeView/EqTreeItem.cs` | Engineer, Blazor Component Specialist | Tree node model — assign to one only |
| `Library/wwwroot/` | Engineer, Blazor Component Specialist | JS interop / CSS — assign by file |

## Spawn Prompt Guidelines

When spawning teammates:
1. **Always specify the role** from the table above
2. **List exact files** the teammate may modify (not just directories)
3. **Include the verification command** -- teammate must run it before reporting done
4. **Reference CLAUDE.md sections** relevant to the role
5. **State dependencies** -- if teammate B needs teammate A's output, say so

## Recommended Team Size

- **Small feature** (single component change): 1-2 (Engineer or Blazor Component Specialist)
- **Medium feature** (cross-cutting): 2-3 (Engineer + Demo App + optionally Test)
- **Large feature** (new component/module): 3-4 (Engineer + Blazor + Demo App + Tests)
- **Utility change**: Utility Library Specialist alone, or paired with Engineer if component changes needed
- **Unfamiliar area / spike**: Researcher / Explorer first, then implementation teammates based on findings
