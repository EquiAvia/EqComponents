# Phase 2: Layout Engine

**Teammate:** Engineer (Opus recommended — algorithmic complexity)
**Depends on:** Phase 1
**Verification:** `dotnet build equiavia.components.sln && dotnet test Tests/equiavia.components.Tests.csproj`

---

### Task 2.1: Write GraphStructureAnalyzer — tests first

**Files:**
- Create: `Tests/Library/GraphView/GraphStructureAnalyzerTests.cs`

**Step 1: Write tests**

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class GraphStructureAnalyzerTests
{
    [Fact]
    public void SingleRoot_NoMultiParent_ReturnsHierarchicalTree()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B"), E("A", "C") };
        Assert.Equal(GraphLayoutMode.HierarchicalTree, GraphStructureAnalyzer.Detect(nodes, edges));
    }

    [Fact]
    public void MultipleRoots_NoMultiParent_ReturnsForest()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("A", "B"), E("C", "D") };
        Assert.Equal(GraphLayoutMode.Forest, GraphStructureAnalyzer.Detect(nodes, edges));
    }

    [Fact]
    public void MultiParentNode_ReturnsDAG()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "C"), E("B", "C") }; // C has two parents
        Assert.Equal(GraphLayoutMode.DAG, GraphStructureAnalyzer.Detect(nodes, edges));
    }

    [Fact]
    public void OnlyUndirectedEdges_ReturnsNetwork()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge>
        {
            new GraphEdge { Id = "e1", SourceNodeId = "A", TargetNodeId = "B", Direction = EdgeDirection.Undirected }
        };
        Assert.Equal(GraphLayoutMode.Network, GraphStructureAnalyzer.Detect(nodes, edges));
    }

    [Fact]
    public void EmptyGraph_ReturnsForest()
    {
        Assert.Equal(GraphLayoutMode.Forest, GraphStructureAnalyzer.Detect(new(), new()));
    }

    [Fact]
    public void SingleNode_NoEdges_ReturnsHierarchicalTree()
    {
        var nodes = new List<GraphNode> { N("A") };
        Assert.Equal(GraphLayoutMode.HierarchicalTree, GraphStructureAnalyzer.Detect(nodes, new()));
    }

    [Fact]
    public void DisconnectedNodes_ReturnsForest()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        Assert.Equal(GraphLayoutMode.Forest, GraphStructureAnalyzer.Detect(nodes, new()));
    }

    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~GraphStructureAnalyzer"`
Expected: Build error — `GraphStructureAnalyzer` does not exist

**Step 3: Commit**

```bash
git add Tests/Library/GraphView/GraphStructureAnalyzerTests.cs
git commit -m "test(graph): add GraphStructureAnalyzer tests (red)"
```

---

### Task 2.2: Implement GraphStructureAnalyzer

**Files:**
- Create: `Library/GraphView/Layout/GraphStructureAnalyzer.cs`

**Step 1: Implement**

```csharp
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal static class GraphStructureAnalyzer
    {
        public static GraphLayoutMode Detect(List<GraphNode> nodes, List<GraphEdge> edges)
        {
            if (nodes.Count == 0)
                return GraphLayoutMode.Forest;

            // Check if all edges are undirected
            if (edges.Count > 0 && edges.All(e => e.Direction == EdgeDirection.Undirected))
                return GraphLayoutMode.Network;

            // For directed edges: count incoming edges per node
            var incomingCount = new Dictionary<string, int>();
            foreach (var node in nodes)
                incomingCount[node.Id] = 0;

            foreach (var edge in edges.Where(e => e.Direction != EdgeDirection.Undirected))
            {
                if (incomingCount.ContainsKey(edge.TargetNodeId))
                    incomingCount[edge.TargetNodeId]++;
            }

            // Check for multi-parent (any node with >1 incoming)
            bool hasMultiParent = incomingCount.Values.Any(c => c > 1);
            if (hasMultiParent)
                return GraphLayoutMode.DAG;

            // Count root nodes (no incoming directed edges)
            int rootCount = incomingCount.Values.Count(c => c == 0);

            if (rootCount <= 1)
                return GraphLayoutMode.HierarchicalTree;

            return GraphLayoutMode.Forest;
        }
    }
}
```

**Step 2: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~GraphStructureAnalyzer"`
Expected: All 7 tests pass

**Step 3: Commit**

```bash
git add Library/GraphView/Layout/GraphStructureAnalyzer.cs
git commit -m "feat(graph): implement GraphStructureAnalyzer auto-detection"
```

---

### Task 2.3: Write HierarchicalTreeLayout — tests first

**Files:**
- Create: `Tests/Library/GraphView/HierarchicalTreeLayoutTests.cs`

**Step 1: Write tests**

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class HierarchicalTreeLayoutTests
{
    private readonly GraphLayoutOptions _opts = new();

    [Fact]
    public void SingleNode_PositionedAtOrigin()
    {
        var nodes = new List<GraphNode> { N("A") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, new(), _opts);
        Assert.Single(result.Nodes);
        Assert.Equal(0, result.Nodes[0].Y); // root at top
    }

    [Fact]
    public void ParentChild_ChildBelowParent()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge> { E("A", "B") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        var parentPos = result.Nodes.First(n => n.Node.Id == "A");
        var childPos = result.Nodes.First(n => n.Node.Id == "B");
        Assert.True(childPos.Y > parentPos.Y, "Child should be below parent");
    }

    [Fact]
    public void TwoChildren_SideBySide()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B"), E("A", "C") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        var b = result.Nodes.First(n => n.Node.Id == "B");
        var c = result.Nodes.First(n => n.Node.Id == "C");
        Assert.NotEqual(b.X, c.X); // siblings should have different X
        Assert.Equal(b.Y, c.Y);    // siblings should have same Y (same level)
    }

    [Fact]
    public void ParentCenteredOverChildren()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B"), E("A", "C") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var b = result.Nodes.First(n => n.Node.Id == "B");
        var c = result.Nodes.First(n => n.Node.Id == "C");
        var childrenMidX = (b.X + c.X) / 2.0;
        Assert.Equal(childrenMidX, parent.X, precision: 1);
    }

    [Fact]
    public void EdgesGenerated_WithValidSvgPaths()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge> { E("A", "B") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        Assert.Single(result.Edges);
        Assert.StartsWith("M", result.Edges[0].SvgPath);
        Assert.Contains("C", result.Edges[0].SvgPath); // cubic bezier
    }

    [Fact]
    public void NoOverlap_BetweenSubtrees()
    {
        // Left subtree: A->B->D, A->B->E
        // Right subtree: A->C->F, A->C->G
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D"), N("E"), N("F"), N("G") };
        var edges = new List<GraphEdge>
        {
            E("A","B"), E("A","C"), E("B","D"), E("B","E"), E("C","F"), E("C","G")
        };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        // Check that no two nodes on the same level overlap
        var byLevel = result.Nodes.GroupBy(n => n.Y);
        foreach (var level in byLevel)
        {
            var sorted = level.OrderBy(n => n.X).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                var gap = sorted[i].X - sorted[i - 1].X;
                Assert.True(gap >= sorted[i - 1].Width,
                    $"Nodes overlap at Y={sorted[i].Y}: gap={gap}, width={sorted[i - 1].Width}");
            }
        }
    }

    [Fact]
    public void TotalDimensions_ArePositive()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B"), E("A", "C") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        Assert.True(result.TotalWidth > 0);
        Assert.True(result.TotalHeight > 0);
    }

    [Fact]
    public void NodeDimensions_ReflectShape()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "A", Label = "A", Shape = NodeShape.Circle },
            new() { Id = "B", Label = "B", Shape = NodeShape.RoundedRectangle }
        };
        var edges = new List<GraphEdge> { E("A", "B") };
        var result = new HierarchicalTreeLayout().Calculate(nodes, edges, _opts);

        var circle = result.Nodes.First(n => n.Node.Id == "A");
        var rect = result.Nodes.First(n => n.Node.Id == "B");
        Assert.Equal(circle.Width, circle.Height); // circle is square
        Assert.True(rect.Width > rect.Height);     // rectangle is wider
    }

    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~HierarchicalTreeLayout"`
Expected: Build error — `HierarchicalTreeLayout` does not exist

**Step 3: Commit**

```bash
git add Tests/Library/GraphView/HierarchicalTreeLayoutTests.cs
git commit -m "test(graph): add HierarchicalTreeLayout tests (red)"
```

---

### Task 2.4: Implement HierarchicalTreeLayout

**Files:**
- Create: `Library/GraphView/Layout/HierarchicalTreeLayout.cs`

**Design notes for implementer:**
- This is the most complex task in the project. Take it step by step.
- The algorithm is a layered tree layout (not full Reingold-Tilford):
  1. Find the root node (node with no incoming edges). If multiple, use the first.
  2. Build a tree structure from the directed edges (source=parent, target=child).
  3. Assign each node to a layer (depth from root). Y = layer * (nodeHeight + verticalSpacing).
  4. Position leaf nodes left-to-right with horizontal spacing.
  5. Center each parent over its children.
  6. Shift subtrees right to eliminate overlap (left-to-right sweep at each level).
  7. Generate cubic bezier SVG paths for edges: `M startX,startY C startX,midY midX,midY endX,endY` where midY is halfway between parent and child.
  8. Calculate label midpoints at the bezier midpoint (average of start and end for simplicity).
- Resolve `NodeShape.Auto` → `RoundedRectangle` for hierarchy mode.
- Circle nodes: width = height = `CircleDiameter`.
- Rectangle/RoundedRectangle nodes: width = `DefaultNodeWidth`, height = `DefaultNodeHeight`.

**Step 1: Implement the layout** (aim for ~150-200 lines)

The implementer should write the class implementing `IGraphLayout`. Key method signature:

```csharp
internal class HierarchicalTreeLayout : IGraphLayout
{
    public LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options)
    {
        // Implementation here
    }
}
```

**Step 2: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~HierarchicalTreeLayout"`
Expected: All 8 tests pass

**Step 3: Commit**

```bash
git add Library/GraphView/Layout/HierarchicalTreeLayout.cs
git commit -m "feat(graph): implement HierarchicalTreeLayout with layered algorithm"
```

---

### Task 2.5: Write ForestLayout — tests first

**Files:**
- Create: `Tests/Library/GraphView/ForestLayoutTests.cs`

**Step 1: Write tests**

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class ForestLayoutTests
{
    private readonly GraphLayoutOptions _opts = new();

    [Fact]
    public void TwoTrees_ArrangedSideBySide()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("A", "B"), E("C", "D") };
        var result = new ForestLayout().Calculate(nodes, edges, _opts);

        // All 4 nodes should be positioned
        Assert.Equal(4, result.Nodes.Count);

        // Tree 1 (A,B) should be left of Tree 2 (C,D)
        var tree1MaxX = result.Nodes.Where(n => n.Node.Id is "A" or "B").Max(n => n.X + n.Width);
        var tree2MinX = result.Nodes.Where(n => n.Node.Id is "C" or "D").Min(n => n.X);
        Assert.True(tree2MinX >= tree1MaxX, "Tree 2 should be to the right of Tree 1");
    }

    [Fact]
    public void TreesOrderedByDataAppearance()
    {
        var nodes = new List<GraphNode> { N("X"), N("Y"), N("A"), N("B") };
        var edges = new List<GraphEdge> { E("X", "Y"), E("A", "B") };
        var result = new ForestLayout().Calculate(nodes, edges, _opts);

        var xPos = result.Nodes.First(n => n.Node.Id == "X").X;
        var aPos = result.Nodes.First(n => n.Node.Id == "A").X;
        Assert.True(xPos < aPos, "First tree in data order should be leftmost");
    }

    [Fact]
    public void SingleTree_DelegatesToHierarchical()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge> { E("A", "B") };
        var result = new ForestLayout().Calculate(nodes, edges, _opts);
        Assert.Equal(2, result.Nodes.Count);
        Assert.Single(result.Edges);
    }

    [Fact]
    public void TotalDimensions_SpanAllTrees()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("A", "B"), E("C", "D") };
        var result = new ForestLayout().Calculate(nodes, edges, _opts);
        Assert.True(result.TotalWidth > 0);
        Assert.True(result.TotalHeight > 0);
    }

    [Fact]
    public void DisconnectedSingleNodes_EachPositioned()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var result = new ForestLayout().Calculate(nodes, new(), _opts);
        Assert.Equal(3, result.Nodes.Count);

        // All should have different X positions
        var xs = result.Nodes.Select(n => n.X).Distinct().ToList();
        Assert.Equal(3, xs.Count);
    }

    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~ForestLayout"`
Expected: Build error

**Step 3: Commit**

```bash
git add Tests/Library/GraphView/ForestLayoutTests.cs
git commit -m "test(graph): add ForestLayout tests (red)"
```

---

### Task 2.6: Implement ForestLayout

**Files:**
- Create: `Library/GraphView/Layout/ForestLayout.cs`

**Step 1: Implement**

```csharp
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal class ForestLayout : IGraphLayout
    {
        public LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options)
        {
            if (nodes.Count == 0)
                return new LayoutResult();

            // Find weakly connected components
            var components = FindComponents(nodes, edges);
            var treeLayout = new HierarchicalTreeLayout();
            var result = new LayoutResult();
            double xOffset = 0;
            double maxHeight = 0;

            foreach (var component in components)
            {
                var componentNodeIds = new HashSet<string>(component.Select(n => n.Id));
                var componentEdges = edges
                    .Where(e => componentNodeIds.Contains(e.SourceNodeId) && componentNodeIds.Contains(e.TargetNodeId))
                    .ToList();

                var treeResult = treeLayout.Calculate(component, componentEdges, options);

                // Offset all nodes in this tree
                foreach (var posNode in treeResult.Nodes)
                {
                    posNode.X += xOffset;
                    result.Nodes.Add(posNode);
                }

                // Offset edge paths and labels
                foreach (var edgePath in treeResult.Edges)
                {
                    if (xOffset > 0)
                    {
                        edgePath.SvgPath = OffsetSvgPath(edgePath.SvgPath, xOffset);
                        edgePath.LabelX += xOffset;
                    }
                    result.Edges.Add(edgePath);
                }

                xOffset += treeResult.TotalWidth + options.HorizontalSpacing;
                maxHeight = Math.Max(maxHeight, treeResult.TotalHeight);
            }

            result.TotalWidth = xOffset - options.HorizontalSpacing; // remove trailing spacing
            result.TotalHeight = maxHeight;
            if (result.TotalWidth < 0) result.TotalWidth = 0;

            return result;
        }

        private static List<List<GraphNode>> FindComponents(List<GraphNode> nodes, List<GraphEdge> edges)
        {
            // Build undirected adjacency for component detection
            var adj = new Dictionary<string, HashSet<string>>();
            foreach (var node in nodes)
                adj[node.Id] = new();
            foreach (var edge in edges)
            {
                if (adj.ContainsKey(edge.SourceNodeId) && adj.ContainsKey(edge.TargetNodeId))
                {
                    adj[edge.SourceNodeId].Add(edge.TargetNodeId);
                    adj[edge.TargetNodeId].Add(edge.SourceNodeId);
                }
            }

            var visited = new HashSet<string>();
            var components = new List<List<GraphNode>>();
            var nodeMap = nodes.ToDictionary(n => n.Id);

            // Process in data order to maintain ordering
            foreach (var node in nodes)
            {
                if (visited.Contains(node.Id)) continue;

                var component = new List<GraphNode>();
                var queue = new Queue<string>();
                queue.Enqueue(node.Id);
                visited.Add(node.Id);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(nodeMap[current]);

                    foreach (var neighbor in adj[current])
                    {
                        if (visited.Add(neighbor))
                            queue.Enqueue(neighbor);
                    }
                }

                components.Add(component);
            }

            return components;
        }

        private static string OffsetSvgPath(string path, double xOffset)
        {
            // Simple offset: adjust all X coordinates in the SVG path
            // Path format: "M x1,y1 C cx1,cy1 cx2,cy2 x2,y2"
            // This is a basic implementation — works for our bezier edge paths
            var parts = path.Split(' ');
            var result = new List<string>();

            foreach (var part in parts)
            {
                if (part is "M" or "C" or "L" or "Q")
                {
                    result.Add(part);
                    continue;
                }

                if (part.Contains(','))
                {
                    var coords = part.Split(',');
                    if (double.TryParse(coords[0], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var x))
                    {
                        coords[0] = (x + xOffset).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    result.Add(string.Join(",", coords));
                }
                else
                {
                    result.Add(part);
                }
            }

            return string.Join(" ", result);
        }
    }
}
```

**Step 2: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "FullyQualifiedName~ForestLayout"`
Expected: All 5 tests pass

**Step 3: Run ALL tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj`
Expected: All tests pass (existing + new)

**Step 4: Commit**

```bash
git add Library/GraphView/Layout/ForestLayout.cs
git commit -m "feat(graph): implement ForestLayout with component detection"
```

---

## Phase 2 Complete Checklist

After all tasks, verify:
- [ ] `dotnet build equiavia.components.sln` succeeds
- [ ] `dotnet test Tests/equiavia.components.Tests.csproj` — all tests pass
- [ ] `Library/GraphView/Layout/` contains: `GraphStructureAnalyzer.cs`, `HierarchicalTreeLayout.cs`, `ForestLayout.cs`
- [ ] `Tests/Library/GraphView/` contains: `GraphStructureAnalyzerTests.cs`, `HierarchicalTreeLayoutTests.cs`, `ForestLayoutTests.cs`
- [ ] Total new tests: ~20 passing tests
