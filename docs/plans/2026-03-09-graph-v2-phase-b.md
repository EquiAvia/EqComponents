# Phase B: Code Review Fixes (Tasks 5-9)

**Role:** Blazor Component Specialist
**Dependencies:** Phase A must be complete (edge routing + direction are referenced in some fixes).

---

## Task 5: Fix OnContextActionSelected to Include Node Context

**Files:**
- Modify: `Library/GraphView/EqGraphView.Razor.cs`

**Step 1: Change EventCallback signature**

In `EqGraphView.Razor.cs`, change line 39:

```csharp
// FROM:
[Parameter] public EventCallback<GraphContextAction> OnContextActionSelected { get; set; }

// TO:
[Parameter] public EventCallback<(GraphNode Node, GraphContextAction Action)> OnContextActionSelected { get; set; }
```

**Step 2: Update HandleContextAction to pass node**

Change the `HandleContextAction` method (around line 300):

```csharp
// FROM:
private async Task HandleContextAction(GraphContextAction action)
{
    _contextMenuVisible = false;
    await OnContextActionSelected.InvokeAsync(action);
}

// TO:
private async Task HandleContextAction(GraphContextAction action)
{
    _contextMenuVisible = false;
    await OnContextActionSelected.InvokeAsync((_contextMenuNode, action));
}
```

**Step 3: Build to verify**

Run: `dotnet build equiavia.components.sln`
Expected: Build FAILS in `Client/Pages/GraphView.razor` because the demo handler signature doesn't match. This is expected — we'll fix the demo page in Task 11.

Verify the Library builds alone: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeds.

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphView.Razor.cs
git commit -m "fix(graph): include node context in OnContextActionSelected callback"
```

---

## Task 6: Remove ExpandAll/CollapseAll, Keep NavigateToRoot

**Files:**
- Modify: `Library/GraphView/EqGraphView.Razor.cs`

**Step 1: Remove ExpandAll and CollapseAll methods**

Delete the `ExpandAll()` and `CollapseAll()` methods (lines 138-152). Keep `NavigateToRoot()` as-is (lines 154-160).

The Public API region should now contain:
- `SetSelectedNode(string nodeId)`
- `Refresh(GraphData data)`
- `ResetView()`
- `NavigateToRoot()`

**Step 2: Build Library to verify**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeds.

Note: `Client/Pages/GraphView.razor` still references `ExpandAll()` — this will be fixed in Task 11.

**Step 3: Commit**

```bash
git add Library/GraphView/EqGraphView.Razor.cs
git commit -m "fix(graph): remove misleading ExpandAll/CollapseAll, keep NavigateToRoot"
```

---

## Task 7: Implement Arrow Key Keyboard Navigation

**Files:**
- Modify: `Library/GraphView/EqGraphView.Razor.cs`

This is the most complex code review fix. Arrow keys need to navigate the graph structure:
- **ArrowDown**: Focus first child of current node
- **ArrowUp**: Focus parent of current node
- **ArrowRight**: Focus next sibling
- **ArrowLeft**: Focus previous sibling
- **Home**: Focus first root node
- **End**: Focus last root node

**Step 1: Add adjacency helper method**

Add a new method to the Helpers region that builds navigation maps from the current sanitized data:

```csharp
private (Dictionary<string, string> parentMap, Dictionary<string, List<string>> childrenMap, List<string> rootIds) BuildNavigationMaps()
{
    var parentMap = new Dictionary<string, string>();
    var childrenMap = new Dictionary<string, List<string>>();
    var incomingSet = new HashSet<string>();

    if (_sanitizedData == null)
        return (parentMap, childrenMap, new List<string>());

    foreach (var node in _sanitizedData.Nodes)
        childrenMap[node.Id] = new List<string>();

    foreach (var edge in _sanitizedData.Edges)
    {
        if (edge.Direction != EdgeDirection.Undirected
            && childrenMap.ContainsKey(edge.SourceNodeId))
        {
            childrenMap[edge.SourceNodeId].Add(edge.TargetNodeId);
            parentMap[edge.TargetNodeId] = edge.SourceNodeId;
            incomingSet.Add(edge.TargetNodeId);
        }
    }

    var rootIds = _sanitizedData.Nodes
        .Where(n => !incomingSet.Contains(n.Id))
        .Select(n => n.Id)
        .ToList();

    return (parentMap, childrenMap, rootIds);
}
```

**Step 2: Update HandleKeyDown with arrow key cases**

Add cases to the existing `switch (e.Key)` in `HandleKeyDown`:

```csharp
case "ArrowDown":
{
    var (_, childrenMap, rootIds) = BuildNavigationMaps();
    if (string.IsNullOrEmpty(_focusedNodeId))
    {
        // No focus — focus first root
        if (rootIds.Count > 0) _focusedNodeId = rootIds[0];
    }
    else if (childrenMap.TryGetValue(_focusedNodeId, out var children) && children.Count > 0)
    {
        _focusedNodeId = children[0];
    }
    break;
}

case "ArrowUp":
{
    var (parentMap, _, _) = BuildNavigationMaps();
    if (!string.IsNullOrEmpty(_focusedNodeId) && parentMap.TryGetValue(_focusedNodeId, out var parentId))
    {
        _focusedNodeId = parentId;
    }
    break;
}

case "ArrowRight":
{
    var (parentMap, childrenMap, rootIds) = BuildNavigationMaps();
    if (!string.IsNullOrEmpty(_focusedNodeId))
    {
        List<string> siblings;
        if (parentMap.TryGetValue(_focusedNodeId, out var parentId))
            siblings = childrenMap[parentId];
        else
            siblings = rootIds;

        int index = siblings.IndexOf(_focusedNodeId);
        if (index >= 0 && index < siblings.Count - 1)
            _focusedNodeId = siblings[index + 1];
    }
    break;
}

case "ArrowLeft":
{
    var (parentMap, childrenMap, rootIds) = BuildNavigationMaps();
    if (!string.IsNullOrEmpty(_focusedNodeId))
    {
        List<string> siblings;
        if (parentMap.TryGetValue(_focusedNodeId, out var parentId))
            siblings = childrenMap[parentId];
        else
            siblings = rootIds;

        int index = siblings.IndexOf(_focusedNodeId);
        if (index > 0)
            _focusedNodeId = siblings[index - 1];
    }
    break;
}

case "Home":
{
    var (_, _, rootIds) = BuildNavigationMaps();
    if (rootIds.Count > 0) _focusedNodeId = rootIds[0];
    break;
}

case "End":
{
    var (_, _, rootIds) = BuildNavigationMaps();
    if (rootIds.Count > 0) _focusedNodeId = rootIds[rootIds.Count - 1];
    break;
}
```

**Step 3: Build to verify**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeds.

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphView.Razor.cs
git commit -m "feat(graph): implement arrow key keyboard navigation for accessibility"
```

---

## Task 8: Add CSS Custom Properties for Theming

**Files:**
- Modify: `Library/GraphView/EqGraphView.razor.css`
- Modify: `Library/GraphView/EqGraphNode.razor.css`
- Modify: `Library/GraphView/EqGraphEdge.razor.css`
- Modify: `Library/GraphView/EqGraphBreadcrumb.razor.css`
- Modify: `Library/GraphView/EqGraphContextMenu.razor.css`

**Step 1: Define CSS custom properties on the container**

In `EqGraphView.razor.css`, update `.eq-graph-container`:

```css
.eq-graph-container {
    --eq-accent: #0078d4;
    --eq-node-bg: #ffffff;
    --eq-node-border: #cccccc;
    --eq-node-border-hover: #888888;
    --eq-node-label: #333333;
    --eq-edge-stroke: #999999;
    --eq-bg: #fafafa;
    --eq-surface: #f8f9fa;
    --eq-border: #dee2e6;
    --eq-text: #333333;
    --eq-text-muted: #999999;

    position: relative;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    border: 1px solid var(--eq-border);
    border-radius: 4px;
    background: var(--eq-bg);
}
```

Update `.eq-graph-empty`:
```css
.eq-graph-empty {
    display: flex;
    align-items: center;
    justify-content: center;
    flex: 1;
    color: var(--eq-text-muted);
    font-size: 14px;
}
```

**Step 2: Update EqGraphNode.razor.css**

```css
.eq-graph-node {
    cursor: pointer;
}

.eq-graph-node-shape {
    fill: var(--eq-node-bg, #ffffff);
    stroke: var(--eq-node-border, #ccc);
    stroke-width: 1.5;
}

.eq-graph-node:hover .eq-graph-node-shape {
    stroke: var(--eq-node-border-hover, #888);
}

.eq-graph-node-selected .eq-graph-node-shape {
    stroke: var(--eq-accent, #0078d4);
    stroke-width: 2;
    filter: drop-shadow(0 2px 4px rgba(0, 120, 212, 0.3));
}

.eq-graph-node-focused .eq-graph-node-shape {
    stroke: var(--eq-accent, #0078d4);
    stroke-width: 2;
    stroke-dasharray: 4 2;
}

::deep .eq-graph-node-label {
    font-size: 12px;
    fill: var(--eq-node-label, #333);
    pointer-events: none;
    user-select: none;
}

.eq-graph-node-status {
    stroke: var(--eq-node-bg, #fff);
    stroke-width: 1;
}

.eq-graph-node-status-ok {
    fill: #2ecc71;
}

.eq-graph-node-status-warning {
    fill: #f39c12;
}

.eq-graph-node-status-error {
    fill: #e74c3c;
}

.eq-graph-node-status-unknown {
    fill: #95a5a6;
}
```

**Step 3: Update EqGraphEdge.razor.css**

```css
.eq-graph-edge-label {
    font-size: 11px;
    pointer-events: none;
    fill: var(--eq-text-muted, #666);
}
```

**Step 4: Update EqGraphBreadcrumb.razor.css**

```css
.eq-graph-breadcrumb {
    padding: 6px 12px;
    background-color: var(--eq-surface, #f8f9fa);
    border-bottom: 1px solid var(--eq-border, #dee2e6);
}

.eq-graph-breadcrumb-list {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    list-style: none;
    margin: 0;
    padding: 0;
    gap: 4px;
}

.eq-graph-breadcrumb-item {
    display: flex;
    align-items: center;
    gap: 4px;
}

.eq-graph-breadcrumb-button {
    background: none;
    border: none;
    color: var(--eq-accent, #0078d4);
    cursor: pointer;
    padding: 2px 4px;
    border-radius: 3px;
    font-size: 13px;
}

.eq-graph-breadcrumb-button:hover {
    background-color: #e8e8e8;
    text-decoration: underline;
}

.eq-graph-breadcrumb-separator {
    color: var(--eq-text-muted, #999);
    font-size: 13px;
}

.eq-graph-breadcrumb-current {
    font-size: 13px;
    font-weight: 600;
    color: var(--eq-text, #333);
}
```

**Step 5: Update EqGraphContextMenu.razor.css**

```css
.eq-graph-context-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 999;
    background: transparent;
}

.eq-graph-context-menu {
    position: fixed;
    z-index: 1000;
    background: var(--eq-node-bg, #ffffff);
    border: 1px solid var(--eq-border, #dee2e6);
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    min-width: 160px;
    padding: 4px 0;
}

.eq-graph-context-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 6px 12px;
    cursor: pointer;
    font-size: 13px;
    color: var(--eq-text, #333);
    position: relative;
}

.eq-graph-context-item:hover {
    background-color: #f0f0f0;
}

.eq-graph-context-separator {
    height: 1px;
    background-color: var(--eq-border, #dee2e6);
    margin: 4px 0;
}

.eq-graph-context-icon {
    width: 16px;
    height: 16px;
}

.eq-graph-context-submenu-parent {
    justify-content: space-between;
}

.eq-graph-context-submenu-arrow {
    font-size: 10px;
    color: var(--eq-text-muted, #999);
}

.eq-graph-context-submenu {
    display: none;
    position: absolute;
    left: 100%;
    top: 0;
    background: var(--eq-node-bg, #ffffff);
    border: 1px solid var(--eq-border, #dee2e6);
    border-radius: 6px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    min-width: 140px;
    padding: 4px 0;
}

.eq-graph-context-submenu-parent:hover > .eq-graph-context-submenu {
    display: block;
}
```

**Step 6: Build to verify**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeds.

**Step 7: Commit**

```bash
git add Library/GraphView/EqGraphView.razor.css Library/GraphView/EqGraphNode.razor.css Library/GraphView/EqGraphEdge.razor.css Library/GraphView/EqGraphBreadcrumb.razor.css Library/GraphView/EqGraphContextMenu.razor.css
git commit -m "feat(graph): add CSS custom properties for theming"
```

---

## Task 9: Fix Performance Mode Visual Differences + ARIA

**Files:**
- Modify: `Library/GraphView/EqGraphNode.razor`
- Modify: `Library/GraphView/EqGraphEdge.razor`
- Modify: `Library/GraphView/EqGraphView.razor`

**Step 1: Fix status indicator radius in perf mode**

In `EqGraphNode.razor`, change the status indicator section (around line 44-51):

```razor
@* Status indicator *@
@if (PositionedNode.Node.Status != NodeStatus.None)
{
    var statusX = (PositionedNode.Width - 8).ToString(CultureInfo.InvariantCulture);
    var statusR = IsPerformanceMode ? "4" : "5";
    <circle cx="@statusX"
            cy="8"
            r="@statusR"
            class="eq-graph-node-status eq-graph-node-status-@PositionedNode.Node.Status.ToString().ToLowerInvariant()" />
}
```

**Step 2: Fix edge stroke width in perf mode**

In `EqGraphEdge.razor`, update all three `<path>` elements to use dynamic stroke-width:

Change `stroke-width="1.5"` to `stroke-width="@(IsPerformanceMode ? "1" : "1.5")"` in all three path elements.

**Step 3: Add role="alert" to performance warning**

In `EqGraphView.razor`, update the performance warning div (line 16):

```razor
<div class="eq-graph-performance-warning" role="alert">
```

**Step 4: Build to verify**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeds.

**Step 5: Commit**

```bash
git add Library/GraphView/EqGraphNode.razor Library/GraphView/EqGraphEdge.razor Library/GraphView/EqGraphView.razor
git commit -m "fix(graph): performance mode visual differences and ARIA alert role"
```
