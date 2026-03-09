# Graph Visualization Component Design
**Date:** 2026-03-09
**Status:** Approved
**Component:** `EqGraphView`

---

## 1. Design Decisions

| # | Decision | Choice | Rationale |
|---|---|---|---|
| 1 | Data models | Concrete (`GraphNode`, `GraphEdge`) | Graph metadata (status, shape, direction, logos) is too rich for generic property binding |
| 2 | MAUI support | Design for it now, test later | Low cost to use pointer events; avoids painful refactor later |
| 3 | Layout engine | Phased — tree/forest in v1, DAG/network later | Hierarchical layouts are well-understood; DAG/force-directed are substantially more complex |
| 4 | Subtree navigation | Internal state + public imperative methods | Navigation is UI concern; parent controls data and selection |
| 5 | DAG node duplication | Deferred — single placement + OnDataWarning | Duplication adds selection sync complexity; defer with clear warning |
| 6 | Zoom/pan | JS interop managing viewport transform | High-frequency DOM concern; pointer events for MAUI compatibility |

---

## 2. Data Models

### Public Models

```csharp
public class GraphNode
{
    public string Id { get; set; }
    public string Label { get; set; }
    public string? LogoUrl { get; set; }
    public NodeStatus Status { get; set; } = NodeStatus.None;
    public NodeShape Shape { get; set; } = NodeShape.Auto;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GraphEdge
{
    public string Id { get; set; }
    public string SourceNodeId { get; set; }
    public string TargetNodeId { get; set; }
    public string? Label { get; set; }
    public EdgeDirection Direction { get; set; } = EdgeDirection.Directed;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GraphData
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphContextAction
{
    public string Id { get; set; }
    public string Label { get; set; }
    public string? IconUrl { get; set; }
    public bool IsSeparator { get; set; } = false;
    public List<GraphContextAction> Children { get; set; } = new(); // One level only
}

public enum NodeStatus { None, Ok, Warning, Error, Unknown }
public enum NodeShape { Auto, Circle, Rectangle, RoundedRectangle }
public enum EdgeDirection { Undirected, Directed, Bidirectional }
public enum GraphLayoutMode { Auto, HierarchicalTree, Forest, DAG, Network }
```

### Internal Models

```csharp
internal class PositionedNode
{
    public GraphNode Node { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

internal class EdgePath
{
    public GraphEdge Edge { get; set; }
    public string SvgPath { get; set; }      // Ready-to-use SVG path data
    public double LabelX { get; set; }        // Midpoint for label placement
    public double LabelY { get; set; }
}

internal class LayoutResult
{
    public List<PositionedNode> Nodes { get; set; } = new();
    public List<EdgePath> Edges { get; set; } = new();
    public double TotalWidth { get; set; }
    public double TotalHeight { get; set; }
}
```

---

## 3. Validation & Sanitization

`GraphDataSanitizer` — static class, runs on every data update:

1. Discard nodes with null/empty `Id` (+ warning)
2. Deduplicate node IDs — first wins (+ warning)
3. Discard edges with null/empty source/target IDs (+ warning)
4. Discard edges referencing missing nodes (+ warning)
5. Discard self-referencing edges (+ warning)
6. Detect cycles — break by discarding back-edges (+ warning)

All warnings emitted via `OnDataWarning`. Returns a new sanitized `GraphData` — never mutates the consumer's input.

---

## 4. Layout Engine

### Pipeline

```
Data → Sanitize → Analyze → Resolve shapes → Pre-process → Layout → Render
```

### Interface

```csharp
internal interface IGraphLayout
{
    LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options);
}

internal class GraphLayoutOptions
{
    public double DefaultNodeWidth { get; set; } = 120;
    public double DefaultNodeHeight { get; set; } = 60;
    public double CircleDiameter { get; set; } = 60;
    public double HorizontalSpacing { get; set; } = 40;
    public double VerticalSpacing { get; set; } = 60;
    public bool IsPerformanceMode { get; set; } = false;
}
```

### Auto-Detection (`GraphStructureAnalyzer`)

Runs on sanitized data (no cycles at this point):

| Detected Structure | Selected Layout |
|---|---|
| Single root, no multi-parent | `HierarchicalTree` |
| Multiple roots, no multi-parent | `Forest` |
| Has multi-parent nodes | `DAG` |
| Has only undirected edges | `Network` |

For v1, `DAG` and `Network` fall back to `Forest` layout with `OnDataWarning`.

### Shape Resolution

`NodeShape.Auto` is resolved to concrete shapes during pre-processing (before layout), since the layout engine needs node dimensions:
- Hierarchy/Forest mode: `Auto` → `RoundedRectangle`
- Network/DAG mode: `Auto` → `Circle`

Resolved shapes stored in a dictionary used by both layout and rendering.

### Pre-Processing (v1)

For multi-parent nodes: select first incoming edge as parent, discard others, emit warning. Converts DAG to tree structure for layout.

### V1 Implementations

**`HierarchicalTreeLayout`** — Layered tree layout:
- Assign nodes to layers by depth from root
- Position nodes within each layer with even spacing
- Center parent over its children
- Shift subtrees to eliminate overlap (left-to-right sweep)
- Generate cubic bezier SVG edge paths (parent bottom-center to child top-center)
- Calculate edge label midpoints

**`ForestLayout`** — Delegates to `HierarchicalTreeLayout` per tree:
- Identify weakly connected components
- Run tree layout on each independently
- Arrange side-by-side in data order (leftmost = first root in data)
- Aggregate into single `LayoutResult`

### Extensibility

New layouts (DAG, force-directed) implement `IGraphLayout`. No changes needed to rendering or public API.

---

## 5. Component Structure

### File Organization

```
Library/
  GraphView/
    EqGraphView.razor
    EqGraphView.Razor.cs
    EqGraphView.razor.css
    EqGraphNode.razor
    EqGraphNode.razor.css
    EqGraphEdge.razor
    EqGraphEdge.razor.css
    EqGraphBreadcrumb.razor
    EqGraphBreadcrumb.razor.css
    EqGraphContextMenu.razor
    EqGraphContextMenu.razor.css
    Models/
      GraphNode.cs
      GraphEdge.cs
      GraphData.cs
      GraphContextAction.cs
      Enums.cs
    Layout/
      IGraphLayout.cs
      LayoutResult.cs
      GraphLayoutOptions.cs
      HierarchicalTreeLayout.cs
      ForestLayout.cs
      GraphStructureAnalyzer.cs
      GraphDataSanitizer.cs
    GraphViewJsInterop.cs
  wwwroot/
    GraphViewJsInterop.js
```

### Public API

```csharp
public partial class EqGraphView : ComponentBase, IAsyncDisposable
{
    // Inputs
    [Parameter] public GraphData? Data { get; set; }
    [Parameter] public GraphLayoutMode LayoutMode { get; set; } = GraphLayoutMode.Auto;
    [Parameter] public string? SelectedNodeId { get; set; }
    [Parameter] public List<GraphContextAction> ContextActions { get; set; } = new();
    [Parameter] public double MinZoom { get; set; } = 0.25;
    [Parameter] public double MaxZoom { get; set; } = 4.0;
    [Parameter] public double InitialZoom { get; set; } = 0.0;
    [Parameter] public int PerformanceThreshold { get; set; } = 500;
    [Parameter] public bool IsLoading { get; set; } = false;
    [Parameter] public string EmptyStateMessage { get; set; } = "No data to display.";
    [Parameter] public string Id { get; set; } = "eq-graph-container";
    [Parameter] public string CSSClasses { get; set; }
    [Parameter] public string AdditionalStyles { get; set; }
    [Parameter] public string Height { get; set; } = "400px";

    // Event Callbacks
    [Parameter] public EventCallback<GraphNode> OnNodeSelected { get; set; }
    [Parameter] public EventCallback<(GraphNode Node, GraphContextAction Action)> OnContextActionSelected { get; set; }
    [Parameter] public EventCallback<GraphNode> OnBreadcrumbNavigated { get; set; }
    [Parameter] public EventCallback OnSelectionCleared { get; set; }
    [Parameter] public EventCallback<string> OnDataWarning { get; set; }

    // Imperative API (via @ref)
    public async Task SetSelectedNode(string nodeId) { }
    public async Task Refresh(GraphData data) { }
    public void ResetView() { }
    public void ExpandAll() { }
    public void CollapseAll() { }
    public void NavigateToRoot() { }
}
```

### Sub-Components

| Component | Responsibility | Context via |
|---|---|---|
| `EqGraphNode` | SVG node group: shape, label, logo, status indicator | `[Parameter]` |
| `EqGraphEdge` | SVG edge path: line, arrowheads, label | `[Parameter]` |
| `EqGraphBreadcrumb` | Breadcrumb navigation bar (HTML, above SVG) | `[Parameter]` |
| `EqGraphContextMenu` | Context menu overlay (HTML, position:fixed) | `[Parameter]` |

Sub-components use `[Parameter]` not `[CascadingParameter]` — they are flat (rendered in a loop), not recursively nested like TreeView items.

### Lifecycle

```csharp
protected override async Task OnParametersSetAsync()
{
    if (Data changed or first render)
    {
        // 1. Sanitize (emit warnings)
        // 2. Analyze structure → resolve layout mode
        // 3. Resolve Auto shapes → concrete shapes
        // 4. Pre-process (DAG→tree if needed)
        // 5. Compute performance mode
        // 6. Run layout → LayoutResult
        // 7. Handle stale selection → OnSelectionCleared
        // 8. Schedule fit-to-canvas via JS
    }
    else if (only SelectedNodeId changed)
    {
        // Update selection state without re-layout
    }
}
```

---

## 6. SVG Rendering

### Structure

```html
<div id="@Id" class="eq-graph-container @CSSClasses" style="height: @Height; @AdditionalStyles">
    <EqGraphBreadcrumb ... />
    @if (_isPerformanceMode) { <div class="eq-graph-perf-warning" role="alert">...</div> }
    @if (IsLoading) { <svg class="eq-graph-spinner">...</svg> }
    @if (no data) { <svg><text>@EmptyStateMessage</text></svg> }

    <svg id="@(Id)-svg" class="eq-graph-svg" width="100%" height="100%">
        <g id="@(Id)-viewport">
            <defs>
                <marker id="@(Id)-arrowhead" ...> ... </marker>
                <marker id="@(Id)-arrowhead-reverse" ...> ... </marker>
            </defs>
            <g class="eq-graph-edges">
                @foreach edge → <EqGraphEdge ... />
            </g>
            <g class="eq-graph-nodes">
                @foreach node → <EqGraphNode ... />
            </g>
        </g>
    </svg>

    <EqGraphContextMenu ... />
</div>
```

Key decisions:
- No dynamic `viewBox` — JS manages all viewport transforms on the `<g id="viewport">`
- SVG marker IDs namespaced with component `Id` to avoid collisions
- Edges render before nodes (SVG paint order = DOM order)
- Context menu is HTML with `position: fixed`, not SVG

### Selection States

| State | CSS Class | Visual |
|---|---|---|
| Default | — | Normal appearance |
| Selected | `eq-graph-node-selected` | 3px accent border, drop shadow |
| Focused | `eq-graph-node-focused` | 2px dashed focus ring |

### Node Label Handling

Labels truncated in C# to max character count proportional to node width before rendering. `Label` falls back to `Id` if null/empty.

---

## 7. Zoom & Pan (JS Interop)

### C# API

```csharp
public class GraphViewJSInterop : IAsyncDisposable
{
    public async Task Initialize(string svgId, string viewportId, double minZoom, double maxZoom) { }
    public async Task ZoomToFit() { }
    public async Task ResetView() { }
    public async Task ScrollToNode(string nodeElementId) { }
    public async ValueTask DisposeAsync() { }
}
```

### JS Module Responsibilities

- **Viewport transform:** Manages `translate(x,y) scale(s)` on the viewport `<g>`
- **Pan:** `pointerdown`/`pointermove`/`pointerup` on SVG background
- **Wheel zoom:** `wheel` event with `{ passive: false }` for `preventDefault`
- **Pinch zoom:** Two-pointer distance tracking
- **Long-press:** 500ms timeout on `pointerdown`, cancelled on move >10px, calls `dotNetRef.invokeMethodAsync('HandleLongPress', nodeId, clientX, clientY)`
- **Animated vs immediate:** `applyTransform()` for user-driven (immediate), `animateToTransform()` for programmatic (requestAnimationFrame interpolation)
- **Cleanup:** `dispose()` removes all event listeners

All pointer events (not mouse events) for MAUI Hybrid compatibility.

---

## 8. Keyboard Navigation & Accessibility

### Focus Management

Roving tabindex — only focused node has `tabindex="0"`, all others `tabindex="-1"`.

### Keyboard Map

| Key | Action |
|---|---|
| `Tab` / `Shift+Tab` | Enter/leave component |
| `Enter` | Select focused node |
| `Space` | Open context menu on focused node |
| `ArrowDown` | Focus first child (hierarchy) |
| `ArrowUp` | Focus parent (hierarchy) |
| `ArrowRight` | Focus next sibling (hierarchy) |
| `ArrowLeft` | Focus previous sibling (hierarchy) |
| `Escape` | Close context menu / navigate up breadcrumb |
| `Home` / `End` | Focus first/last root node |

### Screen Reader

- Nodes: `role="button"` with `aria-label="[Label], [Status] status, [childCount] children"`
- Breadcrumb: `<nav aria-label="Graph breadcrumb">` with `<ol>`
- Context menu: `role="menu"` with `role="menuitem"` / `role="separator"`
- Alerts: Performance warning and data warnings use `role="alert"`

---

## 9. Context Menu

- **Desktop:** `@oncontextmenu` on node SVG groups
- **Touch/MAUI:** Long-press via JS→.NET callback (`HandleLongPress`)
- **Positioning:** `position: fixed` using `ClientX`/`ClientY`, viewport edge clipping
- **Dismissal:** Outside click (transparent overlay), Escape key, or action selected
- **Submenu:** One level only via `Children` property
- **Separators:** `IsSeparator = true` renders `role="separator"` divider

---

## 10. Performance Mode

Triggered when `Data.Nodes.Count > PerformanceThreshold` (default 500):

| Aspect | Full | Performance |
|---|---|---|
| Labels | Full text | Truncated ~12 chars |
| Logos | Rendered | Hidden |
| Edge width | 2px | 1px |
| Edge labels | Rendered | Hidden |
| Status indicators | 6px | 4px |
| Animations | Enabled | Disabled |
| Warning banner | Hidden | Shown |

---

## 11. DI Registration

Extends existing `EqComponents.Initialize()`:

```csharp
public static void Initialize(IServiceCollection services)
{
    services.AddScoped<TreeViewJSInterop, TreeViewJSInterop>();
    services.AddScoped<GraphViewJSInterop, GraphViewJSInterop>();
}
```

---

## 12. CSS Theming

All customizable via CSS custom properties:

```css
--eq-accent: #2196F3;
--eq-node-bg: #2F5170;
--eq-node-text: #FFFFFF;
--eq-edge-color: #999999;
--eq-status-ok: #4CAF50;
--eq-status-warning: #FF9800;
--eq-status-error: #F44336;
--eq-status-unknown: #9E9E9E;
--eq-menu-bg: #FFFFFF;
--eq-menu-text: #333333;
--eq-breadcrumb-bg: #F5F5F5;
```

---

## 13. Testing Strategy

Requires `[assembly: InternalsVisibleTo("equiavia.components.Tests")]` in Library project.

### Unit Tests (no rendering)

| Area | Key Tests |
|---|---|
| `GraphDataSanitizer` | Null IDs, duplicates, orphan edges, self-refs, cycle breaking, clean passthrough |
| `GraphStructureAnalyzer` | Single root, multiple roots, multi-parent, empty data |
| `HierarchicalTreeLayout` | Positioning, child spacing, overlap avoidance, edge paths |
| `ForestLayout` | Side-by-side arrangement, data order, dimension aggregation |
| Shape resolution | Auto resolves per layout mode, explicit shapes preserved |

### Component Tests (bUnit)

| Area | Key Tests |
|---|---|
| Rendering | SVG nodes/edges rendered, empty state, loading spinner |
| Selection | Click fires event, parameter selects node, stale selection clears |
| Context menu | Right-click, actions rendered, separators |
| Breadcrumbs | Drill-down, breadcrumb click event |
| Performance mode | Threshold triggers simplified rendering |
| Public API | SetSelectedNode, Refresh, ResetView |

---

## 14. Demo Page

`Client/Pages/GraphView.razor` — exercises full API with:
- Sample datasets: hierarchy (org chart), forest (3 trees), DAG (fallback demo)
- Controls: layout mode selector, expand/collapse, reset view, toggle loading, clear data
- Event display: selected node info, warning log
- Context actions: "View Details", separator, "Expand/Collapse Subtree"

---

## 15. Out of Scope (V1)

- DAG layout algorithm
- Force-directed (network) layout algorithm
- Node duplication for multi-parent nodes
- In-component node/edge editing
- Session persistence
- Submenus deeper than one level
- Export to image/PDF
- Full MAUI test validation

---

## 16. Future Versions

| Version | Additions |
|---|---|
| v2 | DAG layout, node duplication with synced selection |
| v3 | Force-directed network layout, edge bundling |
| v4 | MAUI device testing, touch gesture refinement |
