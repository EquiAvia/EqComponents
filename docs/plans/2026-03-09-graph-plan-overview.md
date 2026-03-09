# EqGraphView Implementation Plan — Overview

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build an interactive SVG graph visualization component (`EqGraphView`) for the EqComponents library.

**Architecture:** Layered design — data models → sanitizer → layout engine → SVG rendering → JS interop for zoom/pan. See `docs/plans/2026-03-09-graph-visualization-design.md` for full design.

**Tech Stack:** Blazor (.NET 10), SVG, C#, xUnit + bUnit, JS interop (ES6 modules)

## Phase Structure

The plan is split into 5 phases, each in its own file. Phases are sequential but tasks within a phase can be parallelized across teammates.

| Phase | File | Teammate Role | Depends On |
|---|---|---|---|
| 1 | `graph-plan-phase1-models.md` | Engineer | — |
| 2 | `graph-plan-phase2-layout.md` | Engineer | Phase 1 |
| 3 | `graph-plan-phase3-rendering.md` | Blazor Component Specialist | Phase 2 |
| 4 | `graph-plan-phase4-interactivity.md` | Blazor Component Specialist + Engineer | Phase 3 |
| 5 | `graph-plan-phase5-integration.md` | Demo App Specialist + Test Specialist | Phase 4 |

## Model per task

| Phase | Model | Rationale |
|---|---|---|
| 1 (Models + Sanitizer) | Sonnet | Pattern-following, concrete models from spec |
| 2 (Layout Engine) | Opus | Complex algorithm design, multi-file reasoning |
| 3 (SVG Rendering) | Sonnet | Blazor component patterns, markup-heavy |
| 4 (Interactivity) | Opus | JS interop, event coordination, platform concerns |
| 5 (Integration) | Sonnet | Demo page, wiring, mechanical |

## Resuming After Interruption

1. **Check what's already committed:**
   ```bash
   git log --oneline -20
   ```
   Match commit messages to task numbers (each task ends with a commit).

2. **Check for uncommitted partial work:**
   ```bash
   git status
   git diff
   ```

3. **Resume from the next incomplete task.**

4. **Key landmarks:**
   - Phase 1 done: `Library/GraphView/Models/` exists with all model classes + `GraphDataSanitizer.cs`
   - Phase 2 done: `Library/GraphView/Layout/` exists with working tree/forest layout
   - Phase 3 done: `EqGraphView.razor` + sub-components render SVG from layout results
   - Phase 4 done: Zoom/pan JS interop, context menu, keyboard nav all working
   - Phase 5 done: Demo page, DI registration, all tests green
