# Phase 1: Data Models, Enums & Sanitizer

**Teammate:** Engineer
**Model:** Sonnet
**Depends on:** Nothing
**Verification:** `dotnet build equiavia.components.sln && dotnet test Tests/equiavia.components.Tests.csproj`

---

### Task 1.1: Create Enums

**Files:**
- Create: `Library/GraphView/Models/Enums.cs`

**Step 1: Create the enums file**

```csharp
namespace equiavia.components.Library.GraphView.Models
{
    public enum NodeStatus { None, Ok, Warning, Error, Unknown }
    public enum NodeShape { Auto, Circle, Rectangle, RoundedRectangle }
    public enum EdgeDirection { Undirected, Directed, Bidirectional }
    public enum GraphLayoutMode { Auto, HierarchicalTree, Forest, DAG, Network }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/GraphView/Models/Enums.cs
git commit -m "feat(graph): add graph enums — NodeStatus, NodeShape, EdgeDirection, GraphLayoutMode"
```

---

### Task 1.2: Create GraphNode model

**Files:**
- Create: `Library/GraphView/Models/GraphNode.cs`

**Step 1: Create the model**

```csharp
namespace equiavia.components.Library.GraphView.Models
{
    public class GraphNode
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public NodeStatus Status { get; set; } = NodeStatus.None;
        public NodeShape Shape { get; set; } = NodeShape.Auto;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/GraphView/Models/GraphNode.cs
git commit -m "feat(graph): add GraphNode model"
```

---

### Task 1.3: Create GraphEdge model

**Files:**
- Create: `Library/GraphView/Models/GraphEdge.cs`

**Step 1: Create the model**

```csharp
namespace equiavia.components.Library.GraphView.Models
{
    public class GraphEdge
    {
        public string Id { get; set; } = string.Empty;
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string? Label { get; set; }
        public EdgeDirection Direction { get; set; } = EdgeDirection.Directed;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/GraphView/Models/GraphEdge.cs
git commit -m "feat(graph): add GraphEdge model"
```

---

### Task 1.4: Create GraphData and GraphContextAction models

**Files:**
- Create: `Library/GraphView/Models/GraphData.cs`
- Create: `Library/GraphView/Models/GraphContextAction.cs`

**Step 1: Create GraphData**

```csharp
namespace equiavia.components.Library.GraphView.Models
{
    public class GraphData
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }
}
```

**Step 2: Create GraphContextAction**

```csharp
namespace equiavia.components.Library.GraphView.Models
{
    public class GraphContextAction
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public bool IsSeparator { get; set; } = false;
        public List<GraphContextAction> Children { get; set; } = new();
    }
}
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/Models/GraphData.cs Library/GraphView/Models/GraphContextAction.cs
git commit -m "feat(graph): add GraphData and GraphContextAction models"
```

---

### Task 1.5: Create internal layout result models

**Files:**
- Create: `Library/GraphView/Layout/LayoutResult.cs`
- Create: `Library/GraphView/Layout/GraphLayoutOptions.cs`

**Step 1: Create LayoutResult with PositionedNode and EdgePath**

```csharp
namespace equiavia.components.Library.GraphView.Layout
{
    internal class PositionedNode
    {
        public Models.GraphNode Node { get; set; } = default!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    internal class EdgePath
    {
        public Models.GraphEdge Edge { get; set; } = default!;
        public string SvgPath { get; set; } = string.Empty;
        public double LabelX { get; set; }
        public double LabelY { get; set; }
    }

    internal class LayoutResult
    {
        public List<PositionedNode> Nodes { get; set; } = new();
        public List<EdgePath> Edges { get; set; } = new();
        public double TotalWidth { get; set; }
        public double TotalHeight { get; set; }
    }
}
```

**Step 2: Create GraphLayoutOptions**

```csharp
namespace equiavia.components.Library.GraphView.Layout
{
    internal class GraphLayoutOptions
    {
        public double DefaultNodeWidth { get; set; } = 120;
        public double DefaultNodeHeight { get; set; } = 60;
        public double CircleDiameter { get; set; } = 60;
        public double HorizontalSpacing { get; set; } = 40;
        public double VerticalSpacing { get; set; } = 60;
        public bool IsPerformanceMode { get; set; } = false;
    }
}
```

**Step 3: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/Layout/LayoutResult.cs Library/GraphView/Layout/GraphLayoutOptions.cs
git commit -m "feat(graph): add internal layout result models and options"
```

---

### Task 1.6: Add InternalsVisibleTo for testing

**Files:**
- Create: `Library/Properties/AssemblyInfo.cs`

**Step 1: Create AssemblyInfo.cs**

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("equiavia.components.Tests")]
```

**Step 2: Build**

Run: `dotnet build equiavia.components.sln`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/Properties/AssemblyInfo.cs
git commit -m "feat(graph): add InternalsVisibleTo for test project"
```

---

### Task 1.7: Write GraphDataSanitizer — tests first

**Files:**
- Create: `Tests/Library/GraphView/GraphDataSanitizerTests.cs`

**Step 1: Write the test class**

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class GraphDataSanitizerTests
{
    private readonly List<string> _warnings = new();

    private GraphData Sanitize(GraphData data) =>
        GraphDataSanitizer.Sanitize(data, w => _warnings.Add(w));

    [Fact]
    public void CleanData_PassesThrough_Unchanged()
    {
        var data = new GraphData
        {
            Nodes = new() { new GraphNode { Id = "A", Label = "A" }, new GraphNode { Id = "B", Label = "B" } },
            Edges = new() { new GraphEdge { Id = "e1", SourceNodeId = "A", TargetNodeId = "B" } }
        };
        var result = Sanitize(data);
        Assert.Equal(2, result.Nodes.Count);
        Assert.Single(result.Edges);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void NullOrEmptyNodeIds_AreDiscarded()
    {
        var data = new GraphData
        {
            Nodes = new()
            {
                new GraphNode { Id = "", Label = "Empty" },
                new GraphNode { Id = null!, Label = "Null" },
                new GraphNode { Id = "A", Label = "Valid" }
            }
        };
        var result = Sanitize(data);
        Assert.Single(result.Nodes);
        Assert.Equal("A", result.Nodes[0].Id);
        Assert.Equal(2, _warnings.Count);
    }

    [Fact]
    public void DuplicateNodeIds_FirstWins()
    {
        var data = new GraphData
        {
            Nodes = new()
            {
                new GraphNode { Id = "A", Label = "First" },
                new GraphNode { Id = "A", Label = "Second" }
            }
        };
        var result = Sanitize(data);
        Assert.Single(result.Nodes);
        Assert.Equal("First", result.Nodes[0].Label);
        Assert.Single(_warnings);
    }

    [Fact]
    public void EdgesWithNullOrEmptyIds_AreDiscarded()
    {
        var data = new GraphData
        {
            Nodes = new() { new GraphNode { Id = "A" }, new GraphNode { Id = "B" } },
            Edges = new()
            {
                new GraphEdge { Id = "e1", SourceNodeId = "", TargetNodeId = "B" },
                new GraphEdge { Id = "e2", SourceNodeId = "A", TargetNodeId = "" },
                new GraphEdge { Id = "e3", SourceNodeId = "A", TargetNodeId = "B" }
            }
        };
        var result = Sanitize(data);
        Assert.Single(result.Edges);
        Assert.NotEmpty(_warnings);
    }

    [Fact]
    public void OrphanEdges_AreDiscarded()
    {
        var data = new GraphData
        {
            Nodes = new() { new GraphNode { Id = "A" } },
            Edges = new() { new GraphEdge { Id = "e1", SourceNodeId = "A", TargetNodeId = "Z" } }
        };
        var result = Sanitize(data);
        Assert.Empty(result.Edges);
        Assert.Single(_warnings);
    }

    [Fact]
    public void SelfReferencingEdges_AreDiscarded()
    {
        var data = new GraphData
        {
            Nodes = new() { new GraphNode { Id = "A" } },
            Edges = new() { new GraphEdge { Id = "e1", SourceNodeId = "A", TargetNodeId = "A" } }
        };
        var result = Sanitize(data);
        Assert.Empty(result.Edges);
        Assert.Single(_warnings);
    }

    [Fact]
    public void Cycles_AreBrokenByRemovingBackEdge()
    {
        var data = new GraphData
        {
            Nodes = new()
            {
                new GraphNode { Id = "A" },
                new GraphNode { Id = "B" },
                new GraphNode { Id = "C" }
            },
            Edges = new()
            {
                new GraphEdge { Id = "e1", SourceNodeId = "A", TargetNodeId = "B" },
                new GraphEdge { Id = "e2", SourceNodeId = "B", TargetNodeId = "C" },
                new GraphEdge { Id = "e3", SourceNodeId = "C", TargetNodeId = "A" }  // back edge
            }
        };
        var result = Sanitize(data);
        Assert.Equal(2, result.Edges.Count);
        Assert.Single(_warnings);
    }

    [Fact]
    public void NullData_ReturnsEmptyGraphData()
    {
        var result = Sanitize(null!);
        Assert.NotNull(result);
        Assert.Empty(result.Nodes);
        Assert.Empty(result.Edges);
    }

    [Fact]
    public void EmptyData_ReturnsEmptyGraphData()
    {
        var result = Sanitize(new GraphData());
        Assert.Empty(result.Nodes);
        Assert.Empty(result.Edges);
        Assert.Empty(_warnings);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~GraphDataSanitizer"`
Expected: Build error — `GraphDataSanitizer` does not exist

**Step 3: Commit**

```bash
git add Tests/Library/GraphView/GraphDataSanitizerTests.cs
git commit -m "test(graph): add GraphDataSanitizer tests (red)"
```

---

### Task 1.8: Implement GraphDataSanitizer

**Files:**
- Create: `Library/GraphView/Layout/GraphDataSanitizer.cs`

**Step 1: Implement the sanitizer**

```csharp
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal static class GraphDataSanitizer
    {
        public static GraphData Sanitize(GraphData? data, Action<string> onWarning)
        {
            if (data == null)
                return new GraphData();

            var result = new GraphData();

            // 1. Filter nodes with null/empty IDs
            var seenIds = new HashSet<string>();
            foreach (var node in data.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    onWarning($"Node with label '{node.Label}' has no ID and was discarded.");
                    continue;
                }
                if (!seenIds.Add(node.Id))
                {
                    onWarning($"Duplicate node ID '{node.Id}' discarded (first instance kept).");
                    continue;
                }
                result.Nodes.Add(node);
            }

            // 2. Filter edges
            var validNodeIds = seenIds;
            var validEdges = new List<GraphEdge>();
            foreach (var edge in data.Edges)
            {
                if (string.IsNullOrWhiteSpace(edge.SourceNodeId) || string.IsNullOrWhiteSpace(edge.TargetNodeId))
                {
                    onWarning($"Edge '{edge.Id}' has empty source or target and was discarded.");
                    continue;
                }
                if (edge.SourceNodeId == edge.TargetNodeId)
                {
                    onWarning($"Edge '{edge.Id}' is self-referencing and was discarded.");
                    continue;
                }
                if (!validNodeIds.Contains(edge.SourceNodeId) || !validNodeIds.Contains(edge.TargetNodeId))
                {
                    onWarning($"Edge '{edge.Id}' references missing node(s) and was discarded.");
                    continue;
                }
                validEdges.Add(edge);
            }

            // 3. Detect and break cycles via DFS
            result.Edges = RemoveCyclicEdges(result.Nodes, validEdges, onWarning);

            return result;
        }

        private static List<GraphEdge> RemoveCyclicEdges(
            List<GraphNode> nodes, List<GraphEdge> edges, Action<string> onWarning)
        {
            // Build adjacency list
            var adjacency = new Dictionary<string, List<(string Target, GraphEdge Edge)>>();
            foreach (var node in nodes)
                adjacency[node.Id] = new();
            foreach (var edge in edges)
            {
                if (adjacency.ContainsKey(edge.SourceNodeId))
                    adjacency[edge.SourceNodeId].Add((edge.TargetNodeId, edge));
            }

            var result = new List<GraphEdge>(edges);
            var visited = new HashSet<string>();
            var inStack = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (!visited.Contains(node.Id))
                    DfsFindBackEdges(node.Id, adjacency, visited, inStack, result, onWarning);
            }

            return result;
        }

        private static void DfsFindBackEdges(
            string nodeId,
            Dictionary<string, List<(string Target, GraphEdge Edge)>> adjacency,
            HashSet<string> visited,
            HashSet<string> inStack,
            List<GraphEdge> edges,
            Action<string> onWarning)
        {
            visited.Add(nodeId);
            inStack.Add(nodeId);

            if (adjacency.TryGetValue(nodeId, out var neighbors))
            {
                foreach (var (target, edge) in neighbors.ToList())
                {
                    if (inStack.Contains(target))
                    {
                        // Back edge — remove it to break the cycle
                        edges.Remove(edge);
                        onWarning($"Edge '{edge.Id}' ({edge.SourceNodeId}→{edge.TargetNodeId}) creates a cycle and was removed.");
                    }
                    else if (!visited.Contains(target))
                    {
                        DfsFindBackEdges(target, adjacency, visited, inStack, edges, onWarning);
                    }
                }
            }

            inStack.Remove(nodeId);
        }
    }
}
```

**Step 2: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~GraphDataSanitizer"`
Expected: All 8 tests pass

**Step 3: Commit**

```bash
git add Library/GraphView/Layout/GraphDataSanitizer.cs
git commit -m "feat(graph): implement GraphDataSanitizer with cycle detection"
```

---

### Task 1.9: Create IGraphLayout interface

**Files:**
- Create: `Library/GraphView/Layout/IGraphLayout.cs`

**Step 1: Create the interface**

```csharp
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal interface IGraphLayout
    {
        LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options);
    }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/GraphView/Layout/IGraphLayout.cs
git commit -m "feat(graph): add IGraphLayout interface"
```

---

## Phase 1 Complete Checklist

After all tasks, verify:
- [ ] `dotnet build equiavia.components.sln` succeeds
- [ ] `dotnet test Tests/equiavia.components.Tests.csproj` — all tests pass
- [ ] `Library/GraphView/Models/` contains: `Enums.cs`, `GraphNode.cs`, `GraphEdge.cs`, `GraphData.cs`, `GraphContextAction.cs`
- [ ] `Library/GraphView/Layout/` contains: `LayoutResult.cs`, `GraphLayoutOptions.cs`, `GraphDataSanitizer.cs`, `IGraphLayout.cs`
- [ ] `Tests/Library/GraphView/GraphDataSanitizerTests.cs` exists with 8 passing tests
