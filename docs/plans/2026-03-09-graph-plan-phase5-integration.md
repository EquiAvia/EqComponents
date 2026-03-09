# Phase 5: Demo Page, DI, & Component Tests

**Teammates:** Demo App Specialist + Test Specialist (can run in parallel)
**Model:** Sonnet
**Depends on:** Phase 4
**Verification:** `dotnet build equiavia.components.sln && dotnet test Tests/equiavia.components.Tests.csproj`

---

## Demo App Specialist Tasks

### Task 5.1: Create demo page

**Files:**
- Create: `Client/Pages/GraphView.razor`

**Step 1: Create the demo page**

```razor
@page "/graphview"
@using equiavia.components.Library.GraphView
@using equiavia.components.Library.GraphView.Models

<h3>Graph Visualization Demo</h3>

<div style="display: flex; gap: 8px; margin-bottom: 12px; flex-wrap: wrap;">
    <button @onclick="LoadHierarchy">Hierarchy</button>
    <button @onclick="LoadForest">Forest</button>
    <button @onclick="LoadDAG">DAG (fallback)</button>
    <button @onclick="LoadLarge">Large (perf mode)</button>
    <button @onclick="ClearData">Clear</button>
    <button @onclick="() => _isLoading = !_isLoading">Toggle Loading</button>
    <button @onclick="() => _graphView?.ResetView()">Reset View</button>
    <button @onclick="() => _graphView?.ExpandAll()">Expand All</button>

    <select @bind="_layoutMode">
        <option value="@GraphLayoutMode.Auto">Auto</option>
        <option value="@GraphLayoutMode.HierarchicalTree">Hierarchy</option>
        <option value="@GraphLayoutMode.Forest">Forest</option>
    </select>
</div>

@if (_selectedNode != null)
{
    <div style="background: #e3f2fd; padding: 8px 12px; border-radius: 4px; margin-bottom: 8px;">
        <strong>Selected:</strong> @_selectedNode.Label
        (Status: @_selectedNode.Status, Shape: @_selectedNode.Shape)
        <button @onclick="() => _graphView?.SetSelectedNode(_selectedNode.Id)" style="margin-left: 8px;">
            Scroll to selected
        </button>
    </div>
}

@if (_warnings.Count > 0)
{
    <div style="background: #FFF3E0; padding: 8px 12px; border-radius: 4px; margin-bottom: 8px; max-height: 100px; overflow-y: auto;">
        <strong>Warnings:</strong>
        @foreach (var w in _warnings)
        {
            <div style="font-size: 12px;">@w</div>
        }
    </div>
}

<EqGraphView @ref="_graphView"
             Data="_graphData"
             LayoutMode="_layoutMode"
             SelectedNodeId="@_selectedNode?.Id"
             ContextActions="_contextActions"
             IsLoading="_isLoading"
             Height="500px"
             OnNodeSelected="HandleNodeSelected"
             OnContextActionSelected="HandleContextAction"
             OnBreadcrumbNavigated="HandleBreadcrumb"
             OnSelectionCleared="HandleSelectionCleared"
             OnDataWarning="HandleWarning" />

@code {
    private EqGraphView? _graphView;
    private GraphData? _graphData;
    private GraphLayoutMode _layoutMode = GraphLayoutMode.Auto;
    private GraphNode? _selectedNode;
    private bool _isLoading;
    private List<string> _warnings = new();

    private List<GraphContextAction> _contextActions = new()
    {
        new() { Id = "details", Label = "View Details" },
        new() { Id = "sep", IsSeparator = true },
        new() { Id = "expand", Label = "Expand Subtree" },
        new() { Id = "collapse", Label = "Collapse Subtree" },
    };

    protected override void OnInitialized()
    {
        LoadHierarchy();
    }

    private void LoadHierarchy()
    {
        _warnings.Clear();
        _selectedNode = null;
        _graphData = new GraphData
        {
            Nodes = new()
            {
                new() { Id = "ceo", Label = "CEO", Status = NodeStatus.Ok },
                new() { Id = "cto", Label = "CTO", Status = NodeStatus.Ok },
                new() { Id = "cfo", Label = "CFO", Status = NodeStatus.Warning },
                new() { Id = "dev1", Label = "Dev Lead", Status = NodeStatus.Ok },
                new() { Id = "dev2", Label = "Sr Developer", Status = NodeStatus.Ok },
                new() { Id = "dev3", Label = "Jr Developer", Status = NodeStatus.Error },
                new() { Id = "qa", Label = "QA Lead", Status = NodeStatus.Ok },
                new() { Id = "fin1", Label = "Accountant", Status = NodeStatus.None },
                new() { Id = "fin2", Label = "Analyst", Status = NodeStatus.Unknown },
            },
            Edges = new()
            {
                new() { Id = "e1", SourceNodeId = "ceo", TargetNodeId = "cto" },
                new() { Id = "e2", SourceNodeId = "ceo", TargetNodeId = "cfo" },
                new() { Id = "e3", SourceNodeId = "cto", TargetNodeId = "dev1" },
                new() { Id = "e4", SourceNodeId = "cto", TargetNodeId = "qa" },
                new() { Id = "e5", SourceNodeId = "dev1", TargetNodeId = "dev2" },
                new() { Id = "e6", SourceNodeId = "dev1", TargetNodeId = "dev3" },
                new() { Id = "e7", SourceNodeId = "cfo", TargetNodeId = "fin1" },
                new() { Id = "e8", SourceNodeId = "cfo", TargetNodeId = "fin2" },
            }
        };
    }

    private void LoadForest()
    {
        _warnings.Clear();
        _selectedNode = null;
        _graphData = new GraphData
        {
            Nodes = new()
            {
                new() { Id = "t1r", Label = "Team Alpha", Status = NodeStatus.Ok, Shape = NodeShape.RoundedRectangle },
                new() { Id = "t1a", Label = "Alice" },
                new() { Id = "t1b", Label = "Bob" },
                new() { Id = "t2r", Label = "Team Beta", Status = NodeStatus.Warning, Shape = NodeShape.RoundedRectangle },
                new() { Id = "t2a", Label = "Charlie" },
                new() { Id = "t2b", Label = "Diana" },
                new() { Id = "t3r", Label = "Team Gamma", Status = NodeStatus.Error, Shape = NodeShape.RoundedRectangle },
                new() { Id = "t3a", Label = "Eve" },
            },
            Edges = new()
            {
                new() { Id = "f1", SourceNodeId = "t1r", TargetNodeId = "t1a" },
                new() { Id = "f2", SourceNodeId = "t1r", TargetNodeId = "t1b" },
                new() { Id = "f3", SourceNodeId = "t2r", TargetNodeId = "t2a" },
                new() { Id = "f4", SourceNodeId = "t2r", TargetNodeId = "t2b" },
                new() { Id = "f5", SourceNodeId = "t3r", TargetNodeId = "t3a" },
            }
        };
    }

    private void LoadDAG()
    {
        _warnings.Clear();
        _selectedNode = null;
        _graphData = new GraphData
        {
            Nodes = new()
            {
                new() { Id = "a", Label = "Shared Dep" },
                new() { Id = "b", Label = "Service A" },
                new() { Id = "c", Label = "Service B" },
                new() { Id = "d", Label = "Gateway" },
            },
            Edges = new()
            {
                new() { Id = "d1", SourceNodeId = "d", TargetNodeId = "b" },
                new() { Id = "d2", SourceNodeId = "d", TargetNodeId = "c" },
                new() { Id = "d3", SourceNodeId = "b", TargetNodeId = "a" },
                new() { Id = "d4", SourceNodeId = "c", TargetNodeId = "a" }, // multi-parent
            }
        };
    }

    private void LoadLarge()
    {
        _warnings.Clear();
        _selectedNode = null;
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        nodes.Add(new GraphNode { Id = "root", Label = "Root", Status = NodeStatus.Ok });

        for (int i = 0; i < 600; i++)
        {
            var id = $"n{i}";
            nodes.Add(new GraphNode { Id = id, Label = $"Node {i}" });
            var parentId = i < 10 ? "root" : $"n{i / 10}";
            edges.Add(new GraphEdge { Id = $"e{i}", SourceNodeId = parentId, TargetNodeId = id });
        }

        _graphData = new GraphData { Nodes = nodes, Edges = edges };
    }

    private void ClearData()
    {
        _graphData = null;
        _selectedNode = null;
        _warnings.Clear();
    }

    private void HandleNodeSelected(GraphNode node)
    {
        _selectedNode = node;
    }

    private void HandleContextAction((GraphNode Node, GraphContextAction Action) args)
    {
        _warnings.Add($"Context action '{args.Action.Label}' on '{args.Node.Label}'");
    }

    private void HandleBreadcrumb(GraphNode node)
    {
        _warnings.Add($"Navigated to breadcrumb: {node.Label}");
    }

    private void HandleSelectionCleared()
    {
        _selectedNode = null;
    }

    private void HandleWarning(string warning)
    {
        _warnings.Add(warning);
    }
}
```

**Step 2: Build**

Run: `dotnet build equiavia.components.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Client/Pages/GraphView.razor
git commit -m "feat(graph): add GraphView demo page with hierarchy, forest, DAG, and large datasets"
```

---

### Task 5.2: Add navigation link to demo

**Files:**
- Modify: `Client/Shared/NavMenu.razor` (or equivalent navigation file)

**Step 1: Find and update the nav menu**

Add a link to the graph view demo alongside the existing TreeView link. Look at the pattern used for the TreeView nav link and replicate it.

**Step 2: Build & run**

Run: `dotnet build equiavia.components.sln`
Then: `dotnet run --project Server/equiavia.components.Server.csproj`
Verify: Navigate to `/graphview` in the browser and confirm the page renders.

**Step 3: Commit**

```bash
git add Client/Shared/NavMenu.razor
git commit -m "feat(graph): add GraphView link to demo navigation"
```

---

## Test Specialist Tasks (can run in parallel with 5.1-5.2)

### Task 5.3: Write EqGraphView component tests

**Files:**
- Create: `Tests/Library/GraphView/EqGraphViewTests.cs`

**Step 1: Write component tests**

Follow the pattern from `Tests/Library/EqTreeViewTests.cs` — use bUnit with loose JS interop:

```csharp
using Bunit;
using equiavia.components.Library.GraphView;
using equiavia.components.Library.GraphView.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class EqGraphViewTests : BunitContext
{
    private sealed class TestGraphJSInterop : GraphViewJSInterop, IDisposable
    {
        public TestGraphJSInterop(IJSRuntime jsRuntime) : base(jsRuntime) { }
        public void Dispose() { }
    }

    public EqGraphViewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/equiavia.components.Library/GraphViewJsInterop.js");
        Services.AddScoped<GraphViewJSInterop, TestGraphJSInterop>();
    }

    private static GraphData SimpleTree() => new()
    {
        Nodes = new()
        {
            new() { Id = "A", Label = "Root" },
            new() { Id = "B", Label = "Child" },
        },
        Edges = new() { new() { Id = "e1", SourceNodeId = "A", TargetNodeId = "B" } }
    };

    [Fact]
    public void EmptyState_ShowsMessage()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, null)
            .Add(x => x.EmptyStateMessage, "Nothing here"));
        Assert.Contains("Nothing here", cut.Markup);
    }

    [Fact]
    public void LoadingState_ShowsSpinner()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.IsLoading, true));
        Assert.Contains("eq-graph-spinner", cut.Markup);
    }

    [Fact]
    public void WithData_RendersNodes()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree()));
        Assert.Contains("eq-graph-node", cut.Markup);
    }

    [Fact]
    public void WithData_RendersEdges()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree()));
        Assert.Contains("eq-graph-edge", cut.Markup);
    }

    [Fact]
    public void SelectedNodeId_HighlightsNode()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.SelectedNodeId, "A"));
        Assert.Contains("eq-graph-node-selected", cut.Markup);
    }

    [Fact]
    public void NodeClick_FiresOnNodeSelected()
    {
        GraphNode? selected = null;
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.OnNodeSelected, (GraphNode n) => { selected = n; }));

        // Find a node element and click it
        var nodeElements = cut.FindAll(".eq-graph-node");
        if (nodeElements.Count > 0)
        {
            nodeElements[0].Click();
            Assert.NotNull(selected);
        }
    }

    [Fact]
    public void EmptyData_NoSvgRendered()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, new GraphData()));
        Assert.DoesNotContain("eq-graph-edges", cut.Markup);
    }

    [Fact]
    public void PerformanceMode_ShowsWarning()
    {
        var largeData = new GraphData();
        for (int i = 0; i < 10; i++)
            largeData.Nodes.Add(new GraphNode { Id = $"n{i}", Label = $"Node {i}" });

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, largeData)
            .Add(x => x.PerformanceThreshold, 5));
        Assert.Contains("eq-graph-perf-warning", cut.Markup);
    }

    [Fact]
    public void MalformedData_EmitsWarning()
    {
        var warnings = new List<string>();
        var data = new GraphData
        {
            Nodes = new() { new() { Id = "A", Label = "A" } },
            Edges = new() { new() { Id = "e1", SourceNodeId = "A", TargetNodeId = "Z" } } // orphan
        };

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, data)
            .Add(x => x.OnDataWarning, (string w) => warnings.Add(w)));

        Assert.NotEmpty(warnings);
    }

    [Fact]
    public void CustomId_AppliedToContainer()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.Id, "my-graph"));
        Assert.Contains("my-graph", cut.Markup);
    }
}
```

**Step 2: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~EqGraphView"`
Expected: All tests pass

**Step 3: Run ALL tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj`
Expected: All tests pass (existing TreeView tests + all new graph tests)

**Step 4: Commit**

```bash
git add Tests/Library/GraphView/EqGraphViewTests.cs
git commit -m "test(graph): add EqGraphView component tests with bUnit"
```

---

### Task 5.4: Update package metadata

**Files:**
- Modify: `Library/equiavia.components.Library.csproj`

**Step 1: Update PackageTags**

Add "GraphView" to the tags:

```xml
<PackageTags>Treeview, GraphView, Blazor, .net10</PackageTags>
```

**Step 2: Build**

Run: `dotnet build equiavia.components.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/equiavia.components.Library.csproj
git commit -m "chore: add GraphView to NuGet package tags"
```

---

## Phase 5 Complete Checklist

After all tasks, verify:
- [ ] `dotnet build equiavia.components.sln` succeeds
- [ ] `dotnet test Tests/equiavia.components.Tests.csproj` — ALL tests pass
- [ ] Demo app: `dotnet run --project Server/equiavia.components.Server.csproj` — navigate to `/graphview`
- [ ] Hierarchy, Forest, DAG (fallback), and Large dataset demos all render
- [ ] Context menu opens on right-click
- [ ] Selection highlights correctly
- [ ] Zoom (scroll wheel) and pan (drag) work
- [ ] Breadcrumb navigation works in hierarchy mode
- [ ] Performance mode triggers on large dataset (warning banner visible)
- [ ] Warnings display for DAG data (fallback message)

---

## Final Summary

| Phase | Tasks | New Tests | Key Output |
|---|---|---|---|
| 1 | 9 tasks | ~8 | Models, enums, sanitizer |
| 2 | 6 tasks | ~20 | Layout engine (tree + forest) |
| 3 | 6 tasks | 0 (markup) | SVG components |
| 4 | 4 tasks | 0 (interop) | Zoom/pan/context menu |
| 5 | 4 tasks | ~10 | Demo page, component tests |
| **Total** | **29 tasks** | **~38** | **Complete EqGraphView v1** |
