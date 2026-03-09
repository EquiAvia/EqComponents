# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

EqComponents is a Blazor component library published as a NuGet package (`equiavia.components.Library`). It provides `EqTreeView<TValue>` (hierarchical tree) and `EqGraphView` (SVG graph visualization). The solution contains a reusable component library, a utility extensions library, a demo Blazor WebAssembly app, and a test project.

## Build Commands

```bash
# Build the solution
dotnet build equiavia.components.sln

# Release build (auto-generates NuGet packages for Library and Utilities)
dotnet build equiavia.components.sln -c Release

# Run the demo application
dotnet run --project Server/equiavia.components.Server.csproj
```

## Test Commands

```bash
# Run all tests (128 tests across TreeView and GraphView)
dotnet test equiavia.components.sln --verbosity minimal
```

There are no linting commands in this repository.

## Solution Structure

- **Library/** — Main Razor component library (NuGet package, `equiavia.components.Library`)
- **equiavia.components.Utilities/** — Extension methods library (NuGet package, `equiavia.components.Utilities`)
- **Client/** — Blazor WebAssembly demo app; `Client/Pages/TreeView.razor` and `Client/Pages/GraphView.razor` demonstrate component features
- **Server/** — ASP.NET Core host for the WebAssembly demo
- **Shared/** — Shared data models (e.g., `Employee.cs`)
- **Tests/** — xUnit + bUnit test project (`equiavia.components.Tests`); Library exposes internals via `InternalsVisibleTo`

## Architecture

### Component Library (`Library/TreeView/`)

The library exports a single generic component `EqTreeView<TValue>` that renders hierarchical data from a flat list.

**Key files:**
- `EqTreeView.Razor.cs` — All component logic: tree building, CRUD operations, filtering, selection
- `EqTreeView.razor` — Blazor markup using the `Virtualize` component for performance
- `EqTreeViewItem.razor` — Recursive item renderer; receives parent context via `[CascadingParameter]`
- `EqTreeItem.cs` — Internal tree node model wrapping the user's `TValue` data
- `TreeViewJsInterop.cs` + `wwwroot/TreeViewJsInterop.js` — JS interop for scrolling a node into view

### Graph Visualization Component (`Library/GraphView/`)

`EqGraphView` renders interactive SVG graph visualizations supporting trees, forests, DAGs, and networks. Unlike TreeView, it uses concrete models (not generics/reflection).

**Key files:**
- `EqGraphView.Razor.cs` — Component logic: data pipeline, selection, keyboard navigation
- `EqGraphView.razor` — SVG markup (no viewBox — JS manages viewport via transforms)
- `EqGraphNode.razor` / `EqGraphEdge.razor` — SVG sub-components for nodes and edges
- `EqGraphBreadcrumb.razor` / `EqGraphContextMenu.razor` — HTML overlay components
- `GraphViewJSInterop.cs` + `wwwroot/GraphViewJsInterop.js` — JS interop for zoom/pan/pinch
- `Models/` — `GraphNode`, `GraphEdge`, `GraphData`, `GraphContextAction`, enums
- `Layout/` — `IGraphLayout` interface, `HierarchicalTreeLayout`, `ForestLayout`, `GraphStructureAnalyzer`, `GraphDataSanitizer`

### Tree Building Pattern

`EqTreeView<TValue>` accepts a flat `List<TValue>` datasource and builds a hierarchy using three configurable property names:
- `KeyPropertyName` (default: `"Id"`) — unique identifier
- `ValuePropertyName` (default: `"Name"`) — display label
- `ParentKeyPropertyName` (default: `"ParentId"`) — reference to parent; `null` = root node

Property values are read from `TValue` instances at runtime via reflection (via `ClassIntrospectExtensions`).

### Utility Library (`equiavia.components.Utilities/`)

Provides reflection-based extension methods used internally by the library:
- `GetPropValue` / `SetPropValue` — read/write object properties by name (supports dotted paths like `"Address.City"`)
- `ShallowCopyPropertiesFrom/To` — property-level object mapping
- `HasProperty` — property existence check

### Dependency Injection

Consumers must call `EqComponents.Initialize(services)` in their `Program.cs` or `Startup.cs`. This registers `TreeViewJSInterop` and `GraphViewJSInterop` as scoped services.

### NuGet Packaging

Both `Library` and `equiavia.components.Utilities` have `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`, so Release builds automatically produce `.nupkg` files. Version numbers are coordinated across all projects and should be updated together.

## Component Public API

`EqTreeView<TValue>` exposes the following imperative methods (accessed via `@ref`):

| Method | Description |
|---|---|
| `Add(TValue item)` | Add an item to the tree |
| `Update(TValue item)` | Update an existing item (handles reparenting) |
| `Remove(TValue item)` | Remove item and all descendants |
| `Filter(string term, bool caseSensitive)` | Filter visible items by label |
| `SetSelectedItem(TValue item)` | Programmatically select an item |
| `ShowItem(TValue item)` | Expand ancestors and scroll item into view |
| `ExpandAll()` / `CollapseAll()` | Control all expansion states |
| `Refresh(List<TValue> data)` | Replace the entire datasource |

## Code Conventions

- Component prefix: `Eq` (e.g., `EqTreeView`, `EqGraphView`)
- JS interop: Lazy-loaded ES6 modules via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", ...)`, disposed with `IAsyncDisposable`
- Library does NOT enable `<Nullable>` — use runtime null checks, not `?` annotations
- Library does NOT enable `<ImplicitUsings>` — add explicit `using` statements
- Internal types exposed to tests via `[assembly: InternalsVisibleTo("equiavia.components.Tests")]` in `Library/Properties/AssemblyInfo.cs`
