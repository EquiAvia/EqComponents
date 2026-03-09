# Phase C: Wiring, Demo, Tests (Tasks 10-12)

**Role:** Blazor Component Specialist (Task 10), Demo App Specialist (Task 11), Test Specialist (Task 12)
**Dependencies:** Phase A and Phase B must be complete.

---

## Task 10: Wire New Parameters into EqGraphView Component

**Files:**
- Modify: `Library/GraphView/EqGraphView.Razor.cs`

**Step 1: Add Direction, Routing, and CornerRadius parameters**

Add to the Parameters region (after the existing `Height` parameter):

```csharp
[Parameter] public LayoutDirection Direction { get; set; } = LayoutDirection.TopToBottom;
[Parameter] public EdgeRouting Routing { get; set; } = EdgeRouting.Bezier;
[Parameter] public double CornerRadius { get; set; } = 8;
```

Add the `using` for the enums if not already present (they're in `Models` namespace which is already imported).

**Step 2: Pass new options to RunLayout**

In the `RunLayout()` method, update the `GraphLayoutOptions` construction to include the new properties:

```csharp
var options = new GraphLayoutOptions
{
    IsPerformanceMode = _isPerformanceMode,
    Direction = Direction,
    EdgeRouting = Routing,
    CornerRadius = CornerRadius
};
```

**Step 3: Update arrowhead marker orientation**

In `EqGraphView.razor`, the arrowhead markers need to work correctly for all layout directions. The current markers use `orient="auto-start-reverse"` which should handle direction changes automatically since the SVG path direction determines marker orientation. **No changes needed here** — verify by visual testing.

**Step 4: Build to verify**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeds.

**Step 5: Commit**

```bash
git add Library/GraphView/EqGraphView.Razor.cs
git commit -m "feat(graph): wire Direction, Routing, CornerRadius parameters to layout pipeline"
```

---

## Task 11: Update Demo Page

**Files:**
- Modify: `Client/Pages/GraphView.razor`

**Step 1: Fix OnContextActionSelected handler signature**

Update the handler to match the new tuple signature:

```csharp
// FROM:
private void HandleContextAction(GraphContextAction action)
{
    eventsFired.Add($"Context action: {action.Label} on {selectedNode?.Label ?? "none"}");
}

// TO:
private void HandleContextAction((GraphNode Node, GraphContextAction Action) args)
{
    eventsFired.Add($"Context action: {args.Action.Label} on {args.Node?.Label ?? "none"}");
}
```

**Step 2: Replace ExpandAll button with NavigateToRoot**

Change the ExpandAll button:

```razor
@* FROM: *@
<button class="btn btn-outline-primary btn-sm" @onclick="ExpandAll">Expand All</button>

@* TO: *@
<button class="btn btn-outline-primary btn-sm" @onclick="NavigateToRoot">Navigate to Root</button>
```

Update the method:

```csharp
// FROM:
private async Task ExpandAll()
{
    if (graphView != null)
    {
        await graphView.ExpandAll();
    }
    eventsFired.Add("Expand all");
}

// TO:
private async Task NavigateToRoot()
{
    if (graphView != null)
    {
        await graphView.NavigateToRoot();
    }
    eventsFired.Add("Navigate to root");
}
```

**Step 3: Add Direction and Routing parameters to EqGraphView**

Update the `<EqGraphView>` component tag to include the new parameters:

```razor
<EqGraphView
    @ref="graphView"
    Data="@graphData"
    LayoutMode="@layoutMode"
    Direction="@direction"
    Routing="@routing"
    CornerRadius="@cornerRadius"
    SelectedNodeId="@selectedNodeId"
    ContextActions="@contextActions"
    IsLoading="@isLoading"
    PerformanceThreshold="500"
    Height="500px"
    OnNodeSelected="HandleNodeSelected"
    OnContextActionSelected="HandleContextAction"
    OnDataWarning="HandleWarning"
    OnSelectionCleared="HandleSelectionCleared" />
```

**Step 4: Add state fields and dropdown controls**

Add to the `@code` block fields:

```csharp
private LayoutDirection direction = LayoutDirection.TopToBottom;
private EdgeRouting routing = EdgeRouting.Bezier;
private double cornerRadius = 8;
```

Add change handlers:

```csharp
private void DirectionChanged(ChangeEventArgs e)
{
    if (Enum.TryParse<LayoutDirection>(e.Value?.ToString(), out var dir))
    {
        direction = dir;
    }
}

private void RoutingChanged(ChangeEventArgs e)
{
    if (Enum.TryParse<EdgeRouting>(e.Value?.ToString(), out var r))
    {
        routing = r;
    }
}

private void CornerRadiusChanged(ChangeEventArgs e)
{
    if (double.TryParse(e.Value?.ToString(), out var r))
    {
        cornerRadius = r;
    }
}
```

Add dropdowns to the Controls section (after the Layout Mode dropdown):

```razor
<label>
    Direction:
    <select @onchange="DirectionChanged">
        <option value="TopToBottom" selected="@(direction == LayoutDirection.TopToBottom)">Top → Bottom</option>
        <option value="BottomToTop" selected="@(direction == LayoutDirection.BottomToTop)">Bottom → Top</option>
        <option value="LeftToRight" selected="@(direction == LayoutDirection.LeftToRight)">Left → Right</option>
        <option value="RightToLeft" selected="@(direction == LayoutDirection.RightToLeft)">Right → Left</option>
    </select>
</label>
<label>
    Edge Routing:
    <select @onchange="RoutingChanged">
        <option value="Bezier" selected="@(routing == EdgeRouting.Bezier)">Bezier (Curved)</option>
        <option value="Straight" selected="@(routing == EdgeRouting.Straight)">Straight</option>
        <option value="Orthogonal" selected="@(routing == EdgeRouting.Orthogonal)">Orthogonal (Right-angle)</option>
    </select>
</label>
@if (routing == EdgeRouting.Orthogonal)
{
    <label>
        Corner Radius:
        <input type="range" min="0" max="20" step="1" value="@cornerRadius" @onchange="CornerRadiusChanged" />
        @cornerRadius px
    </label>
}
```

**Step 5: Build and verify**

Run: `dotnet build equiavia.components.sln`
Expected: Full solution builds successfully.

**Step 6: Commit**

```bash
git add Client/Pages/GraphView.razor
git commit -m "feat(graph): update demo page with direction, routing controls and fixed API"
```

---

## Task 12: Add Tests for New Features

**Files:**
- Modify: `Tests/Library/GraphView/EqGraphViewTests.cs`
- Tests created in Phase A already cover layout direction and edge routing

**Step 1: Add component-level tests for new parameters**

Add these tests to `EqGraphViewTests.cs`:

```csharp
[Fact]
public void DirectionParameter_Accepted()
{
    var cut = Render<EqGraphView>(p => p
        .Add(x => x.Data, SimpleTree())
        .Add(x => x.Direction, LayoutDirection.LeftToRight));

    // Should render without errors
    Assert.Contains("eq-graph-node", cut.Markup);
}

[Fact]
public void RoutingParameter_Accepted()
{
    var cut = Render<EqGraphView>(p => p
        .Add(x => x.Data, SimpleTree())
        .Add(x => x.Routing, EdgeRouting.Straight));

    Assert.Contains("eq-graph-edge", cut.Markup);
}

[Fact]
public void CornerRadiusParameter_Accepted()
{
    var cut = Render<EqGraphView>(p => p
        .Add(x => x.Data, SimpleTree())
        .Add(x => x.Routing, EdgeRouting.Orthogonal)
        .Add(x => x.CornerRadius, 12));

    Assert.Contains("eq-graph-edge", cut.Markup);
}

[Fact]
public void PerformanceWarning_HasAlertRole()
{
    var largeData = new GraphData();
    for (int i = 0; i < 510; i++)
    {
        largeData.Nodes.Add(new GraphNode { Id = $"n{i}", Label = $"Node {i}" });
        if (i > 0)
            largeData.Edges.Add(new GraphEdge
            {
                Id = $"e{i}",
                SourceNodeId = "n0",
                TargetNodeId = $"n{i}"
            });
    }

    var cut = Render<EqGraphView>(p => p
        .Add(x => x.Data, largeData)
        .Add(x => x.PerformanceThreshold, 500));

    var warning = cut.Find(".eq-graph-performance-warning");
    Assert.Equal("alert", warning.GetAttribute("role"));
}

[Fact]
public void ContextAction_IncludesNodeInCallback()
{
    (GraphNode Node, GraphContextAction Action)? result = null;
    var actions = new List<GraphContextAction>
    {
        new GraphContextAction { Id = "test", Label = "Test Action" }
    };

    var cut = Render<EqGraphView>(p => p
        .Add(x => x.Data, SimpleTree())
        .Add(x => x.ContextActions, actions)
        .Add(x => x.OnContextActionSelected, ((GraphNode Node, GraphContextAction Action) args) => { result = args; }));

    // Trigger context menu on a node
    var node = cut.Find(".eq-graph-node");
    node.ContextMenu();

    // Click the action in the context menu
    var menuItem = cut.Find(".eq-graph-context-item");
    menuItem.Click();

    Assert.NotNull(result);
    Assert.NotNull(result.Value.Node);
    Assert.Equal("test", result.Value.Action.Id);
}
```

**Step 2: Run all tests**

Run: `dotnet test equiavia.components.sln -v minimal`
Expected: ALL tests pass. Count should be approximately 148+ (128 original + ~7 direction + ~5 routing + ~5 component + ~3 forest).

**Step 3: Commit**

```bash
git add Tests/Library/GraphView/EqGraphViewTests.cs
git commit -m "test(graph): add tests for direction, routing, ARIA, and context action tuple"
```
