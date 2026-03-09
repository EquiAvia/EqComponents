# Phase 3: SVG Rendering Components

**Teammate:** Blazor Component Specialist
**Model:** Sonnet
**Depends on:** Phase 2
**Verification:** `dotnet build Library/equiavia.components.Library.csproj`

**Reference files:**
- Existing component pattern: `Library/TreeView/EqTreeView.razor`, `Library/TreeView/EqTreeView.Razor.cs`
- Existing CSS pattern: `Library/TreeView/EqTreeViewItem.razor.css`
- Design doc: `docs/plans/2026-03-09-graph-visualization-design.md` (Sections 5, 6)

---

### Task 3.1: Create EqGraphEdge sub-component

**Files:**
- Create: `Library/GraphView/EqGraphEdge.razor`
- Create: `Library/GraphView/EqGraphEdge.razor.css`

**Step 1: Create the Razor markup**

```razor
@namespace equiavia.components.Library.GraphView
@using equiavia.components.Library.GraphView.Layout
@using equiavia.components.Library.GraphView.Models

<g class="eq-graph-edge">
    <path d="@EdgePath.SvgPath"
          stroke="var(--eq-edge-color, #999999)"
          stroke-width="@(IsPerformanceMode ? 1 : 2)"
          fill="none"
          marker-end="@GetMarkerEnd()"
          marker-start="@GetMarkerStart()" />

    @if (!IsPerformanceMode && Edge?.Label != null)
    {
        <text class="eq-graph-edge-label"
              x="@EdgePath.LabelX.ToString(System.Globalization.CultureInfo.InvariantCulture)"
              y="@EdgePath.LabelY.ToString(System.Globalization.CultureInfo.InvariantCulture)"
              text-anchor="middle"
              dominant-baseline="middle">@Edge.Label</text>
    }
</g>

@code {
    [Parameter] public EdgePath EdgePath { get; set; } = default!;
    [Parameter] public bool IsPerformanceMode { get; set; }
    [Parameter] public string ComponentId { get; set; } = "";

    private GraphEdge? Edge => EdgePath?.Edge;

    private string GetMarkerEnd() => Edge?.Direction is EdgeDirection.Directed or EdgeDirection.Bidirectional
        ? $"url(#{ComponentId}-arrowhead)" : "";

    private string GetMarkerStart() => Edge?.Direction == EdgeDirection.Bidirectional
        ? $"url(#{ComponentId}-arrowhead-reverse)" : "";
}
```

**Step 2: Create the scoped CSS**

```css
.eq-graph-edge-label {
    font-size: 11px;
    fill: var(--eq-edge-color, #999999);
    pointer-events: none;
}
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphEdge.razor Library/GraphView/EqGraphEdge.razor.css
git commit -m "feat(graph): add EqGraphEdge SVG sub-component"
```

---

### Task 3.2: Create EqGraphNode sub-component

**Files:**
- Create: `Library/GraphView/EqGraphNode.razor`
- Create: `Library/GraphView/EqGraphNode.razor.css`

**Step 1: Create the Razor markup**

```razor
@namespace equiavia.components.Library.GraphView
@using equiavia.components.Library.GraphView.Layout
@using equiavia.components.Library.GraphView.Models

<g transform="translate(@X, @Y)"
   class="eq-graph-node @(IsSelected ? "eq-graph-node-selected" : "") @(IsFocused ? "eq-graph-node-focused" : "")"
   role="button"
   aria-label="@AriaLabel"
   aria-selected="@IsSelected.ToString().ToLower()"
   tabindex="@(IsFocused ? 0 : -1)"
   @onclick="OnClick"
   @oncontextmenu="OnContextMenu"
   @oncontextmenu:preventDefault="true"
   id="@ElementId">

    @* Shape *@
    @switch (ResolvedShape)
    {
        case NodeShape.Circle:
            <circle r="@(Width / 2)"
                    fill="var(--eq-node-bg, #2F5170)"
                    stroke="currentColor" stroke-width="1" />
            break;

        case NodeShape.Rectangle:
            <rect x="@((-Width / 2).ToString(Inv))" y="@((-Height / 2).ToString(Inv))"
                  width="@Width.ToString(Inv)" height="@Height.ToString(Inv)"
                  fill="var(--eq-node-bg, #2F5170)"
                  stroke="currentColor" stroke-width="1" />
            break;

        default: @* RoundedRectangle *@
            <rect x="@((-Width / 2).ToString(Inv))" y="@((-Height / 2).ToString(Inv))"
                  width="@Width.ToString(Inv)" height="@Height.ToString(Inv)"
                  rx="8" ry="8"
                  fill="var(--eq-node-bg, #2F5170)"
                  stroke="currentColor" stroke-width="1" />
            break;
    }

    @* Status indicator *@
    @if (Node.Status != NodeStatus.None)
    {
        var r = IsPerformanceMode ? 4 : 6;
        <circle class="eq-graph-status eq-graph-status-@Node.Status.ToString().ToLower()"
                r="@r"
                cx="@((Width / 2 - 8).ToString(Inv))"
                cy="@((-Height / 2 + 8).ToString(Inv))" />
    }

    @* Logo (full mode only) *@
    @if (!IsPerformanceMode && Node.LogoUrl != null)
    {
        <image href="@Node.LogoUrl" width="24" height="24"
               x="-12" y="@((-Height / 2 + 12).ToString(Inv))" />
    }

    @* Label *@
    <text class="eq-graph-node-label"
          text-anchor="middle"
          dominant-baseline="middle"
          y="@LabelOffsetY.ToString(Inv)">@DisplayLabel</text>
</g>

@code {
    private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

    [Parameter] public PositionedNode PositionedNode { get; set; } = default!;
    [Parameter] public NodeShape ResolvedShape { get; set; } = NodeShape.RoundedRectangle;
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public bool IsFocused { get; set; }
    [Parameter] public bool IsPerformanceMode { get; set; }
    [Parameter] public EventCallback<GraphNode> OnClick { get; set; }
    [Parameter] public EventCallback<GraphNode> OnContextMenu { get; set; }
    [Parameter] public string ElementId { get; set; } = "";

    private GraphNode Node => PositionedNode.Node;
    private double X => PositionedNode.X;
    private double Y => PositionedNode.Y;
    private double Width => PositionedNode.Width;
    private double Height => PositionedNode.Height;

    private string DisplayLabel
    {
        get
        {
            var label = string.IsNullOrWhiteSpace(Node.Label) ? Node.Id : Node.Label;
            if (IsPerformanceMode && label.Length > 12)
                return label[..12] + "...";
            return label;
        }
    }

    private double LabelOffsetY => Node.LogoUrl != null && !IsPerformanceMode ? 8 : 0;

    private string AriaLabel
    {
        get
        {
            var status = Node.Status != NodeStatus.None ? $", {Node.Status} status" : "";
            return $"{DisplayLabel}{status}";
        }
    }
}
```

**Step 2: Create the scoped CSS**

```css
.eq-graph-node {
    cursor: pointer;
    color: var(--eq-node-bg, #2F5170);
}

.eq-graph-node-selected {
    color: var(--eq-accent, #2196F3);
}

.eq-graph-node-selected > rect,
.eq-graph-node-selected > circle {
    stroke: var(--eq-accent, #2196F3);
    stroke-width: 3px;
    filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
}

.eq-graph-node-focused > rect,
.eq-graph-node-focused > circle {
    stroke: var(--eq-accent, #2196F3);
    stroke-width: 2px;
    stroke-dasharray: 4 2;
}

.eq-graph-node-label {
    font-size: 13px;
    fill: var(--eq-node-text, #FFFFFF);
    pointer-events: none;
    user-select: none;
}

.eq-graph-status-ok { fill: var(--eq-status-ok, #4CAF50); }
.eq-graph-status-warning { fill: var(--eq-status-warning, #FF9800); }
.eq-graph-status-error { fill: var(--eq-status-error, #F44336); }
.eq-graph-status-unknown { fill: var(--eq-status-unknown, #9E9E9E); }
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphNode.razor Library/GraphView/EqGraphNode.razor.css
git commit -m "feat(graph): add EqGraphNode SVG sub-component with shape/status/label rendering"
```

---

### Task 3.3: Create EqGraphBreadcrumb sub-component

**Files:**
- Create: `Library/GraphView/EqGraphBreadcrumb.razor`
- Create: `Library/GraphView/EqGraphBreadcrumb.razor.css`

**Step 1: Create the Razor markup**

```razor
@namespace equiavia.components.Library.GraphView
@using equiavia.components.Library.GraphView.Models

@if (Path.Count > 0)
{
    <nav class="eq-graph-breadcrumb" aria-label="Graph breadcrumb">
        <ol>
            <li>
                <button @onclick="() => OnNavigate.InvokeAsync(null)">Root</button>
            </li>
            @foreach (var node in Path)
            {
                <li>
                    <span class="eq-graph-breadcrumb-separator">/</span>
                    @if (node.Id == Path.Last().Id)
                    {
                        <span class="eq-graph-breadcrumb-current" aria-current="location">@(string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label)</span>
                    }
                    else
                    {
                        <button @onclick="() => OnNavigate.InvokeAsync(node)">@(string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label)</button>
                    }
                </li>
            }
        </ol>
    </nav>
}

@code {
    [Parameter] public List<GraphNode> Path { get; set; } = new();
    [Parameter] public EventCallback<GraphNode?> OnNavigate { get; set; }
}
```

**Step 2: Create the scoped CSS**

```css
.eq-graph-breadcrumb {
    background: var(--eq-breadcrumb-bg, #F5F5F5);
    padding: 6px 12px;
    font-size: 13px;
    border-bottom: 1px solid #ddd;
}

.eq-graph-breadcrumb ol {
    list-style: none;
    display: flex;
    align-items: center;
    margin: 0;
    padding: 0;
    gap: 4px;
}

.eq-graph-breadcrumb li {
    display: flex;
    align-items: center;
    gap: 4px;
}

.eq-graph-breadcrumb button {
    background: none;
    border: none;
    color: var(--eq-accent, #2196F3);
    cursor: pointer;
    padding: 2px 4px;
    font-size: inherit;
    border-radius: 3px;
}

.eq-graph-breadcrumb button:hover {
    text-decoration: underline;
    background: rgba(0, 0, 0, 0.05);
}

.eq-graph-breadcrumb-separator {
    color: #999;
}

.eq-graph-breadcrumb-current {
    font-weight: 600;
    color: #333;
}
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphBreadcrumb.razor Library/GraphView/EqGraphBreadcrumb.razor.css
git commit -m "feat(graph): add EqGraphBreadcrumb navigation component"
```

---

### Task 3.4: Create EqGraphContextMenu sub-component

**Files:**
- Create: `Library/GraphView/EqGraphContextMenu.razor`
- Create: `Library/GraphView/EqGraphContextMenu.razor.css`

**Step 1: Create the Razor markup**

```razor
@namespace equiavia.components.Library.GraphView
@using equiavia.components.Library.GraphView.Models

@if (IsVisible)
{
    <div class="eq-graph-context-overlay" @onclick="() => OnDismiss.InvokeAsync()"></div>

    <div class="eq-graph-context-menu"
         style="left: @(X.ToString(System.Globalization.CultureInfo.InvariantCulture))px; top: @(Y.ToString(System.Globalization.CultureInfo.InvariantCulture))px;"
         role="menu"
         aria-label="Node actions">

        @foreach (var action in Actions)
        {
            @if (action.IsSeparator)
            {
                <div role="separator" class="eq-graph-context-separator"></div>
            }
            else if (action.Children.Count > 0)
            {
                <div class="eq-graph-context-item has-submenu" role="menuitem" aria-haspopup="true">
                    @if (action.IconUrl != null) { <img src="@action.IconUrl" alt="" class="eq-graph-context-icon" /> }
                    <span>@action.Label</span>
                    <span class="eq-graph-context-chevron">&#9656;</span>
                    <div class="eq-graph-context-submenu" role="menu">
                        @foreach (var child in action.Children)
                        {
                            <div class="eq-graph-context-item" role="menuitem"
                                 @onclick="() => SelectAction(child)" @onclick:stopPropagation="true">
                                @if (child.IconUrl != null) { <img src="@child.IconUrl" alt="" class="eq-graph-context-icon" /> }
                                <span>@child.Label</span>
                            </div>
                        }
                    </div>
                </div>
            }
            else
            {
                <div class="eq-graph-context-item" role="menuitem"
                     @onclick="() => SelectAction(action)">
                    @if (action.IconUrl != null) { <img src="@action.IconUrl" alt="" class="eq-graph-context-icon" /> }
                    <span>@action.Label</span>
                </div>
            }
        }
    </div>
}

@code {
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public double X { get; set; }
    [Parameter] public double Y { get; set; }
    [Parameter] public List<GraphContextAction> Actions { get; set; } = new();
    [Parameter] public EventCallback<GraphContextAction> OnActionSelected { get; set; }
    [Parameter] public EventCallback OnDismiss { get; set; }

    private async Task SelectAction(GraphContextAction action)
    {
        await OnActionSelected.InvokeAsync(action);
    }
}
```

**Step 2: Create the scoped CSS**

```css
.eq-graph-context-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 999;
}

.eq-graph-context-menu {
    position: fixed;
    z-index: 1000;
    background: var(--eq-menu-bg, #FFFFFF);
    color: var(--eq-menu-text, #333333);
    border: 1px solid #ddd;
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    padding: 4px 0;
    min-width: 160px;
    font-size: 13px;
}

.eq-graph-context-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 16px;
    cursor: pointer;
    position: relative;
}

.eq-graph-context-item:hover {
    background: rgba(0, 0, 0, 0.06);
}

.eq-graph-context-separator {
    height: 1px;
    background: #e0e0e0;
    margin: 4px 0;
}

.eq-graph-context-icon {
    width: 16px;
    height: 16px;
}

.eq-graph-context-chevron {
    margin-left: auto;
    font-size: 10px;
    color: #999;
}

.has-submenu > .eq-graph-context-submenu {
    display: none;
    position: absolute;
    left: 100%;
    top: 0;
    background: var(--eq-menu-bg, #FFFFFF);
    border: 1px solid #ddd;
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    padding: 4px 0;
    min-width: 140px;
}

.has-submenu:hover > .eq-graph-context-submenu {
    display: block;
}
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphContextMenu.razor Library/GraphView/EqGraphContextMenu.razor.css
git commit -m "feat(graph): add EqGraphContextMenu with submenu support"
```

---

### Task 3.5: Create main EqGraphView component — markup

**Files:**
- Create: `Library/GraphView/EqGraphView.razor`
- Create: `Library/GraphView/EqGraphView.razor.css`

**Step 1: Create the Razor markup**

```razor
@namespace equiavia.components.Library.GraphView
@using equiavia.components.Library.GraphView.Layout
@using equiavia.components.Library.GraphView.Models

<div id="@Id"
     class="eq-graph-container @CSSClasses"
     style="height: @Height; @AdditionalStyles"
     @onkeydown="HandleKeyDown"
     tabindex="-1">

    <EqGraphBreadcrumb Path="_breadcrumbPath" OnNavigate="HandleBreadcrumbNavigate" />

    @if (_isPerformanceMode)
    {
        <div class="eq-graph-perf-warning" role="alert">
            Simplified view &mdash; @(_sanitizedData?.Nodes.Count ?? 0) nodes exceed performance threshold.
        </div>
    }

    @if (IsLoading)
    {
        <div class="eq-graph-loading">
            <svg width="48" height="48" viewBox="0 0 48 48" class="eq-graph-spinner">
                <circle cx="24" cy="24" r="20" fill="none" stroke="var(--eq-accent, #2196F3)"
                        stroke-width="4" stroke-dasharray="80 40" />
            </svg>
        </div>
    }
    else if (_sanitizedData == null || _sanitizedData.Nodes.Count == 0)
    {
        <div class="eq-graph-empty">
            <svg width="100%" height="100%">
                <text x="50%" y="50%" text-anchor="middle" dominant-baseline="middle"
                      fill="#999" font-size="16">@EmptyStateMessage</text>
            </svg>
        </div>
    }
    else if (_layoutResult != null)
    {
        <svg id="@(Id)-svg" class="eq-graph-svg" width="100%" height="100%">
            <g id="@(Id)-viewport">
                <defs>
                    <marker id="@(Id)-arrowhead" markerWidth="10" markerHeight="7"
                            refX="10" refY="3.5" orient="auto" markerUnits="strokeWidth">
                        <polygon points="0 0, 10 3.5, 0 7" fill="var(--eq-edge-color, #999)" />
                    </marker>
                    <marker id="@(Id)-arrowhead-reverse" markerWidth="10" markerHeight="7"
                            refX="0" refY="3.5" orient="auto" markerUnits="strokeWidth">
                        <polygon points="10 0, 0 3.5, 10 7" fill="var(--eq-edge-color, #999)" />
                    </marker>
                </defs>

                <g class="eq-graph-edges">
                    @foreach (var edgePath in _visibleEdges)
                    {
                        <EqGraphEdge EdgePath="edgePath"
                                     IsPerformanceMode="_isPerformanceMode"
                                     ComponentId="@Id" />
                    }
                </g>

                <g class="eq-graph-nodes">
                    @foreach (var posNode in _visibleNodes)
                    {
                        <EqGraphNode PositionedNode="posNode"
                                     ResolvedShape="GetResolvedShape(posNode.Node)"
                                     IsSelected="posNode.Node.Id == _activeSelectedNodeId"
                                     IsFocused="posNode.Node.Id == _focusedNodeId"
                                     IsPerformanceMode="_isPerformanceMode"
                                     OnClick="HandleNodeClick"
                                     OnContextMenu="HandleNodeContextMenu"
                                     ElementId="@($"{Id}-node-{posNode.Node.Id}")" />
                    }
                </g>
            </g>
        </svg>
    }

    <EqGraphContextMenu IsVisible="_contextMenuVisible"
                        X="_contextMenuX"
                        Y="_contextMenuY"
                        Actions="ContextActions"
                        OnActionSelected="HandleContextAction"
                        OnDismiss="DismissContextMenu" />
</div>

@code {
    // Computed visible items (for hierarchy drill-down)
    private List<PositionedNode> _visibleNodes => _layoutResult?.Nodes ?? new();
    private List<EdgePath> _visibleEdges => _layoutResult?.Edges ?? new();
}
```

**Step 2: Create the scoped CSS**

```css
.eq-graph-container {
    position: relative;
    overflow: hidden;
    border: 1px solid #ddd;
    border-radius: 4px;
    display: flex;
    flex-direction: column;
}

.eq-graph-svg {
    flex: 1;
    min-height: 0;
}

.eq-graph-perf-warning {
    background: #FFF3E0;
    color: #E65100;
    padding: 6px 12px;
    font-size: 12px;
    text-align: center;
    border-bottom: 1px solid #FFE0B2;
}

.eq-graph-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    flex: 1;
}

.eq-graph-spinner {
    animation: eq-graph-spin 1s linear infinite;
}

@keyframes eq-graph-spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
}

.eq-graph-empty {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
}
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: May have errors — the code-behind doesn't exist yet. That's Task 3.6.

**Step 4: Commit (markup only)**

```bash
git add Library/GraphView/EqGraphView.razor Library/GraphView/EqGraphView.razor.css
git commit -m "feat(graph): add EqGraphView main component markup and styles"
```

---

### Task 3.6: Create EqGraphView code-behind

**Files:**
- Create: `Library/GraphView/EqGraphView.Razor.cs`

**Step 1: Create the component logic**

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace equiavia.components.Library.GraphView
{
    public partial class EqGraphView : ComponentBase, IAsyncDisposable
    {
        // === Parameters ===
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
        [Parameter] public string CSSClasses { get; set; } = "";
        [Parameter] public string AdditionalStyles { get; set; } = "";
        [Parameter] public string Height { get; set; } = "400px";

        // === Event Callbacks ===
        [Parameter] public EventCallback<GraphNode> OnNodeSelected { get; set; }
        [Parameter] public EventCallback<(GraphNode Node, GraphContextAction Action)> OnContextActionSelected { get; set; }
        [Parameter] public EventCallback<GraphNode> OnBreadcrumbNavigated { get; set; }
        [Parameter] public EventCallback OnSelectionCleared { get; set; }
        [Parameter] public EventCallback<string> OnDataWarning { get; set; }

        // === Internal State ===
        private LayoutResult? _layoutResult;
        private GraphLayoutMode _resolvedLayoutMode;
        private bool _isPerformanceMode;
        private string? _activeSelectedNodeId;
        private string? _focusedNodeId;
        private GraphData? _sanitizedData;
        private GraphData? _previousData;
        private Dictionary<string, NodeShape> _resolvedShapes = new();

        // Navigation
        private string? _currentRootNodeId;
        private List<GraphNode> _breadcrumbPath = new();

        // Context menu
        private bool _contextMenuVisible;
        private double _contextMenuX;
        private double _contextMenuY;
        private GraphNode? _contextMenuNode;

        #region Lifecycle

        protected override async Task OnParametersSetAsync()
        {
            if (!ReferenceEquals(Data, _previousData))
            {
                _previousData = Data;
                await ProcessData();
            }
            else if (SelectedNodeId != _activeSelectedNodeId)
            {
                UpdateSelection(SelectedNodeId);
            }
        }

        #endregion

        #region Public API

        public async Task SetSelectedNode(string nodeId)
        {
            UpdateSelection(nodeId);
            StateHasChanged();
        }

        public async Task Refresh(GraphData data)
        {
            Data = data;
            _previousData = data;
            await ProcessData();
            StateHasChanged();
        }

        public void ResetView()
        {
            // Will call JS interop in Phase 4
        }

        public void ExpandAll()
        {
            _currentRootNodeId = null;
            _breadcrumbPath.Clear();
            RecalculateLayout();
            StateHasChanged();
        }

        public void CollapseAll()
        {
            // In hierarchy mode, navigate back to root level
            _currentRootNodeId = null;
            _breadcrumbPath.Clear();
            RecalculateLayout();
            StateHasChanged();
        }

        public void NavigateToRoot()
        {
            _currentRootNodeId = null;
            _breadcrumbPath.Clear();
            RecalculateLayout();
            StateHasChanged();
        }

        #endregion

        #region Data Processing Pipeline

        private async Task ProcessData()
        {
            // 1. Sanitize
            _sanitizedData = GraphDataSanitizer.Sanitize(Data, async w => await EmitWarning(w));

            if (_sanitizedData.Nodes.Count == 0)
            {
                _layoutResult = null;
                return;
            }

            // 2. Analyze structure
            var detectedMode = GraphStructureAnalyzer.Detect(_sanitizedData.Nodes, _sanitizedData.Edges);
            _resolvedLayoutMode = LayoutMode == GraphLayoutMode.Auto ? detectedMode : LayoutMode;

            // Warn if forced mode doesn't match structure
            if (LayoutMode != GraphLayoutMode.Auto && LayoutMode != detectedMode)
            {
                await EmitWarning($"Forced layout '{LayoutMode}' applied to '{detectedMode}' structure. Results may be unexpected.");
            }

            // 3. Resolve shapes
            ResolveShapes();

            // 4. Check performance mode
            _isPerformanceMode = _sanitizedData.Nodes.Count > PerformanceThreshold;

            // 5. Pre-process (DAG→tree if needed)
            // For v1, DAG/Network fall back to Forest with warning
            if (_resolvedLayoutMode is GraphLayoutMode.DAG or GraphLayoutMode.Network)
            {
                await EmitWarning($"'{_resolvedLayoutMode}' layout is not yet supported. Falling back to Forest layout.");
                _resolvedLayoutMode = GraphLayoutMode.Forest;
            }

            // 6. Run layout
            RecalculateLayout();

            // 7. Handle stale selection
            if (_activeSelectedNodeId != null && !_sanitizedData.Nodes.Any(n => n.Id == _activeSelectedNodeId))
            {
                _activeSelectedNodeId = null;
                await OnSelectionCleared.InvokeAsync();
            }

            // Apply SelectedNodeId parameter
            if (SelectedNodeId != null)
                UpdateSelection(SelectedNodeId);
        }

        private void RecalculateLayout()
        {
            if (_sanitizedData == null || _sanitizedData.Nodes.Count == 0)
            {
                _layoutResult = null;
                return;
            }

            var options = new GraphLayoutOptions { IsPerformanceMode = _isPerformanceMode };
            IGraphLayout layout = _resolvedLayoutMode switch
            {
                GraphLayoutMode.HierarchicalTree => new HierarchicalTreeLayout(),
                _ => new ForestLayout()
            };

            // If drilled into a subtree, only layout that subtree
            var nodes = _sanitizedData.Nodes;
            var edges = _sanitizedData.Edges;

            if (_currentRootNodeId != null)
            {
                var subtreeNodeIds = GetSubtreeNodeIds(_currentRootNodeId);
                nodes = nodes.Where(n => subtreeNodeIds.Contains(n.Id)).ToList();
                edges = edges.Where(e => subtreeNodeIds.Contains(e.SourceNodeId) && subtreeNodeIds.Contains(e.TargetNodeId)).ToList();
            }

            _layoutResult = layout.Calculate(nodes, edges, options);
        }

        private void ResolveShapes()
        {
            _resolvedShapes.Clear();
            var defaultShape = _resolvedLayoutMode is GraphLayoutMode.HierarchicalTree or GraphLayoutMode.Forest
                ? NodeShape.RoundedRectangle
                : NodeShape.Circle;

            foreach (var node in _sanitizedData!.Nodes)
            {
                _resolvedShapes[node.Id] = node.Shape == NodeShape.Auto ? defaultShape : node.Shape;
            }
        }

        private NodeShape GetResolvedShape(GraphNode node) =>
            _resolvedShapes.TryGetValue(node.Id, out var shape) ? shape : NodeShape.RoundedRectangle;

        #endregion

        #region Selection

        private void UpdateSelection(string? nodeId)
        {
            if (nodeId == _activeSelectedNodeId) return;
            _activeSelectedNodeId = nodeId;
            _focusedNodeId = nodeId;
        }

        #endregion

        #region Event Handlers

        private async Task HandleNodeClick(GraphNode node)
        {
            if (node.Id == _activeSelectedNodeId) return;
            UpdateSelection(node.Id);
            await OnNodeSelected.InvokeAsync(node);
        }

        private void HandleNodeContextMenu(GraphNode node)
        {
            // Context menu position will be set via JS interop in Phase 4
            // For now, store the node
            _contextMenuNode = node;
        }

        private async Task HandleContextAction(GraphContextAction action)
        {
            _contextMenuVisible = false;
            if (_contextMenuNode != null)
            {
                await OnContextActionSelected.InvokeAsync((_contextMenuNode, action));
            }
        }

        private void DismissContextMenu()
        {
            _contextMenuVisible = false;
        }

        private async Task HandleBreadcrumbNavigate(GraphNode? node)
        {
            if (node == null)
            {
                _currentRootNodeId = null;
                _breadcrumbPath.Clear();
            }
            else
            {
                _currentRootNodeId = node.Id;
                var idx = _breadcrumbPath.FindIndex(n => n.Id == node.Id);
                if (idx >= 0)
                    _breadcrumbPath = _breadcrumbPath.Take(idx + 1).ToList();
                await OnBreadcrumbNavigated.InvokeAsync(node);
            }
            RecalculateLayout();
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case "Enter":
                    if (_focusedNodeId != null)
                    {
                        var node = _sanitizedData?.Nodes.FirstOrDefault(n => n.Id == _focusedNodeId);
                        if (node != null) await HandleNodeClick(node);
                    }
                    break;
                case " ":
                    if (_focusedNodeId != null)
                    {
                        _contextMenuNode = _sanitizedData?.Nodes.FirstOrDefault(n => n.Id == _focusedNodeId);
                        if (_contextMenuNode != null && ContextActions.Count > 0)
                        {
                            _contextMenuVisible = true;
                            // Position near focused node — will be refined in Phase 4
                        }
                    }
                    break;
                case "Escape":
                    if (_contextMenuVisible)
                        DismissContextMenu();
                    else if (_breadcrumbPath.Count > 0)
                        await HandleBreadcrumbNavigate(_breadcrumbPath.Count > 1 ? _breadcrumbPath[^2] : null);
                    break;
            }
        }

        #endregion

        #region Helpers

        private HashSet<string> GetSubtreeNodeIds(string rootId)
        {
            var result = new HashSet<string> { rootId };
            var queue = new Queue<string>();
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var edge in _sanitizedData!.Edges.Where(e => e.SourceNodeId == current))
                {
                    if (result.Add(edge.TargetNodeId))
                        queue.Enqueue(edge.TargetNodeId);
                }
            }

            return result;
        }

        private async Task EmitWarning(string message)
        {
            if (OnDataWarning.HasDelegate)
                await OnDataWarning.InvokeAsync(message);
        }

        public async ValueTask DisposeAsync()
        {
            // JS interop cleanup will be added in Phase 4
        }

        #endregion
    }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/GraphView/EqGraphView.Razor.cs
git commit -m "feat(graph): add EqGraphView code-behind with pipeline, selection, navigation"
```

---

## Phase 3 Complete Checklist

After all tasks, verify:
- [ ] `dotnet build equiavia.components.sln` succeeds
- [ ] `Library/GraphView/` contains: `EqGraphView.razor`, `EqGraphView.Razor.cs`, `EqGraphView.razor.css`, `EqGraphNode.razor`, `EqGraphNode.razor.css`, `EqGraphEdge.razor`, `EqGraphEdge.razor.css`, `EqGraphBreadcrumb.razor`, `EqGraphBreadcrumb.razor.css`, `EqGraphContextMenu.razor`, `EqGraphContextMenu.razor.css`
- [ ] All existing tests still pass
