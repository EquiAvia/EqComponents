# Phase A: Layout Engine Changes (Tasks 1-4)

**Role:** Engineer
**Dependencies:** None — this phase touches only `Layout/` and `Models/` internals.

---

## Task 1: Add LayoutDirection and EdgeRouting Enums + Update GraphLayoutOptions

**Files:**
- Modify: `Library/GraphView/Models/Enums.cs`
- Modify: `Library/GraphView/Layout/GraphLayoutOptions.cs`

**Step 1: Add enums to Enums.cs**

Add after the existing `GraphLayoutMode` enum:

```csharp
public enum LayoutDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft
}

public enum EdgeRouting
{
    Bezier,
    Straight,
    Orthogonal
}
```

**Step 2: Add properties to GraphLayoutOptions.cs**

Add three new properties:

```csharp
public LayoutDirection Direction { get; set; } = LayoutDirection.TopToBottom;
public EdgeRouting EdgeRouting { get; set; } = EdgeRouting.Bezier;
public double CornerRadius { get; set; } = 8;
```

**Step 3: Build to verify**

Run: `dotnet build equiavia.components.sln`
Expected: Build succeeds with no errors.

**Step 4: Commit**

```bash
git add Library/GraphView/Models/Enums.cs Library/GraphView/Layout/GraphLayoutOptions.cs
git commit -m "feat(graph): add LayoutDirection, EdgeRouting enums and layout options"
```

---

## Task 2: Extract Edge Path Builder with 3 Routing Strategies

**Files:**
- Modify: `Library/GraphView/Layout/HierarchicalTreeLayout.cs`
- Create: `Tests/Library/GraphView/EdgeRoutingTests.cs`

**Step 1: Write failing tests for edge routing**

Create `Tests/Library/GraphView/EdgeRoutingTests.cs`:

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class EdgeRoutingTests
{
    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };

    private static LayoutResult Layout(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options)
    {
        var layout = new HierarchicalTreeLayout();
        return layout.Calculate(nodes, edges, options);
    }

    [Fact]
    public void BezierRouting_EdgePathContainsCurveCommand()
    {
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Bezier };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        Assert.Single(result.Edges);
        Assert.Contains("C", result.Edges[0].SvgPath); // Cubic bezier
    }

    [Fact]
    public void StraightRouting_EdgePathUsesLineTo()
    {
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Straight };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        Assert.Single(result.Edges);
        var path = result.Edges[0].SvgPath;
        Assert.StartsWith("M", path);
        Assert.Contains("L", path);
        Assert.DoesNotContain("C", path);
    }

    [Fact]
    public void OrthogonalRouting_EdgePathUsesLineSegments()
    {
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Orthogonal };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        Assert.Single(result.Edges);
        var path = result.Edges[0].SvgPath;
        Assert.StartsWith("M", path);
        // Orthogonal paths use L commands (and A for rounded corners)
        Assert.Contains("L", path);
        Assert.DoesNotContain("C", path);
    }

    [Fact]
    public void OrthogonalRouting_WithCornerRadius_ContainsArcCommand()
    {
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Orthogonal, CornerRadius = 8 };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        var path = result.Edges[0].SvgPath;
        Assert.Contains("A", path); // Arc command for rounded corners
    }

    [Fact]
    public void OrthogonalRouting_ZeroCornerRadius_NoArcCommand()
    {
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Orthogonal, CornerRadius = 0 };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        var path = result.Edges[0].SvgPath;
        Assert.DoesNotContain("A", path); // No arc, just line segments
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "EdgeRoutingTests" -v minimal`
Expected: `StraightRouting`, `OrthogonalRouting*` tests FAIL (Bezier test should pass since it's the current default behavior).

**Step 3: Extract BuildEdgePath method and add routing strategies**

In `HierarchicalTreeLayout.cs`, replace the inline edge path generation (lines 99-125) with a call to a new `BuildEdgePath` method. Add the method at the bottom of the class:

Replace the edge generation loop (the `foreach (var edge in edges)` block starting at line 99) with:

```csharp
// Generate edge paths
foreach (var edge in edges)
{
    if (positionedNodes.ContainsKey(edge.SourceNodeId) && positionedNodes.ContainsKey(edge.TargetNodeId))
    {
        var source = positionedNodes[edge.SourceNodeId];
        var target = positionedNodes[edge.TargetNodeId];
        var edgePath = BuildEdgePath(source, target, edge, options);
        result.Edges.Add(edgePath);
    }
}
```

Then add this static method to the class:

```csharp
internal static EdgePath BuildEdgePath(PositionedNode source, PositionedNode target, GraphEdge edge, GraphLayoutOptions options)
{
    // Connection points based on direction
    double startX, startY, endX, endY;

    switch (options.Direction)
    {
        case LayoutDirection.BottomToTop:
            startX = source.X + source.Width / 2;
            startY = source.Y;
            endX = target.X + target.Width / 2;
            endY = target.Y + target.Height;
            break;
        case LayoutDirection.LeftToRight:
            startX = source.X + source.Width;
            startY = source.Y + source.Height / 2;
            endX = target.X;
            endY = target.Y + target.Height / 2;
            break;
        case LayoutDirection.RightToLeft:
            startX = source.X;
            startY = source.Y + source.Height / 2;
            endX = target.X + target.Width;
            endY = target.Y + target.Height / 2;
            break;
        default: // TopToBottom
            startX = source.X + source.Width / 2;
            startY = source.Y + source.Height;
            endX = target.X + target.Width / 2;
            endY = target.Y;
            break;
    }

    string svgPath = options.EdgeRouting switch
    {
        EdgeRouting.Straight => BuildStraightPath(startX, startY, endX, endY),
        EdgeRouting.Orthogonal => BuildOrthogonalPath(startX, startY, endX, endY, options.Direction, options.CornerRadius),
        _ => BuildBezierPath(startX, startY, endX, endY, options.Direction)
    };

    return new EdgePath
    {
        Edge = edge,
        SvgPath = svgPath,
        LabelX = (startX + endX) / 2,
        LabelY = (startY + endY) / 2
    };
}

private static string BuildBezierPath(double startX, double startY, double endX, double endY, LayoutDirection direction)
{
    bool isVertical = direction == LayoutDirection.TopToBottom || direction == LayoutDirection.BottomToTop;

    if (isVertical)
    {
        double midY = (startY + endY) / 2;
        return string.Format(CultureInfo.InvariantCulture,
            "M {0},{1} C {0},{2} {3},{2} {3},{4}",
            startX, startY, midY, endX, endY);
    }
    else
    {
        double midX = (startX + endX) / 2;
        return string.Format(CultureInfo.InvariantCulture,
            "M {0},{1} C {2},{1} {2},{3} {4},{3}",
            startX, startY, midX, endY, endX);
    }
}

private static string BuildStraightPath(double startX, double startY, double endX, double endY)
{
    return string.Format(CultureInfo.InvariantCulture,
        "M {0},{1} L {2},{3}",
        startX, startY, endX, endY);
}

private static string BuildOrthogonalPath(double startX, double startY, double endX, double endY, LayoutDirection direction, double cornerRadius)
{
    bool isVertical = direction == LayoutDirection.TopToBottom || direction == LayoutDirection.BottomToTop;

    if (isVertical)
    {
        // Vertical: go down to midY, turn horizontal, go across, turn down to target
        double midY = (startY + endY) / 2;
        double dx = endX - startX;

        if (Math.Abs(dx) < 0.1 || cornerRadius < 0.1)
        {
            // Straight vertical line or no corner radius
            return string.Format(CultureInfo.InvariantCulture,
                "M {0},{1} L {0},{2} L {3},{2} L {3},{4}",
                startX, startY, midY, endX, endY);
        }

        double r = Math.Min(cornerRadius, Math.Min(Math.Abs(midY - startY), Math.Abs(dx)) / 2);
        double sweepDown = dx > 0 ? 1 : 0; // Sweep flag for first corner
        double sweepUp = dx > 0 ? 0 : 1;   // Sweep flag for second corner
        double signX = dx > 0 ? 1 : -1;

        // First vertical segment → arc → horizontal segment → arc → second vertical segment
        return string.Format(CultureInfo.InvariantCulture,
            "M {0},{1} L {0},{2} A {3},{3} 0 0 {4} {5},{6} L {7},{6} A {3},{3} 0 0 {8} {9},{10} L {9},{11}",
            startX, startY,                                // M and first L: start down
            midY - r,                                       // stop before corner
            r,                                              // arc radius
            sweepDown,                                      // sweep direction
            startX + signX * r, midY,                       // arc end (at midY)
            endX - signX * r, midY,                         // horizontal segment to before second corner
            sweepUp,                                        // sweep direction
            endX, midY + r,                                 // arc end (start going down again)
            endY);                                          // final vertical to target
    }
    else
    {
        // Horizontal: go right to midX, turn vertical, go across, turn right to target
        double midX = (startX + endX) / 2;
        double dy = endY - startY;

        if (Math.Abs(dy) < 0.1 || cornerRadius < 0.1)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "M {0},{1} L {2},{1} L {2},{3} L {4},{3}",
                startX, startY, midX, endY, endX);
        }

        double r = Math.Min(cornerRadius, Math.Min(Math.Abs(midX - startX), Math.Abs(dy)) / 2);
        double sweepRight = dy > 0 ? 0 : 1;
        double sweepLeft = dy > 0 ? 1 : 0;
        double signY = dy > 0 ? 1 : -1;

        return string.Format(CultureInfo.InvariantCulture,
            "M {0},{1} L {2},{1} A {3},{3} 0 0 {4} {5},{6} L {5},{7} A {3},{3} 0 0 {8} {9},{10} L {11},{10}",
            startX, startY,
            midX - r,
            r,
            sweepRight,
            midX, startY + signY * r,
            endY - signY * r,
            sweepLeft,
            midX + r, endY,
            endX);
    }
}
```

**Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "EdgeRoutingTests" -v minimal`
Expected: All 5 tests PASS.

Run: `dotnet test Tests/equiavia.components.Tests.csproj -v minimal`
Expected: All tests PASS (existing tests still work since default routing is Bezier).

**Step 5: Commit**

```bash
git add Library/GraphView/Layout/HierarchicalTreeLayout.cs Tests/Library/GraphView/EdgeRoutingTests.cs
git commit -m "feat(graph): extract BuildEdgePath with Bezier, Straight, Orthogonal routing"
```

---

## Task 3: Add Direction Transform to HierarchicalTreeLayout

**Files:**
- Modify: `Library/GraphView/Layout/HierarchicalTreeLayout.cs`
- Create: `Tests/Library/GraphView/LayoutDirectionTests.cs`

**Step 1: Write failing tests for layout direction**

Create `Tests/Library/GraphView/LayoutDirectionTests.cs`:

```csharp
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class LayoutDirectionTests
{
    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };

    private static LayoutResult Layout(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options)
    {
        var layout = new HierarchicalTreeLayout();
        return layout.Calculate(nodes, edges, options);
    }

    [Fact]
    public void TopToBottom_ChildBelowParent()
    {
        var options = new GraphLayoutOptions { Direction = LayoutDirection.TopToBottom };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var child = result.Nodes.First(n => n.Node.Id == "B");

        Assert.True(child.Y > parent.Y, "Child should be below parent in TB");
    }

    [Fact]
    public void BottomToTop_ChildAboveParent()
    {
        var options = new GraphLayoutOptions { Direction = LayoutDirection.BottomToTop };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var child = result.Nodes.First(n => n.Node.Id == "B");

        Assert.True(child.Y < parent.Y, "Child should be above parent in BT");
    }

    [Fact]
    public void LeftToRight_ChildRightOfParent()
    {
        var options = new GraphLayoutOptions { Direction = LayoutDirection.LeftToRight };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var child = result.Nodes.First(n => n.Node.Id == "B");

        Assert.True(child.X > parent.X, "Child should be right of parent in LR");
        Assert.Equal(parent.Y, child.Y, precision: 1); // Same vertical level for single child
    }

    [Fact]
    public void RightToLeft_ChildLeftOfParent()
    {
        var options = new GraphLayoutOptions { Direction = LayoutDirection.RightToLeft };
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") }, options);

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var child = result.Nodes.First(n => n.Node.Id == "B");

        Assert.True(child.X < parent.X, "Child should be left of parent in RL");
    }

    [Fact]
    public void LeftToRight_TwoChildren_StackedVertically()
    {
        var options = new GraphLayoutOptions { Direction = LayoutDirection.LeftToRight };
        var result = Layout(new() { N("A"), N("B"), N("C") }, new() { E("A", "B"), E("A", "C") }, options);

        var b = result.Nodes.First(n => n.Node.Id == "B");
        var c = result.Nodes.First(n => n.Node.Id == "C");

        Assert.Equal(b.X, c.X, precision: 1); // Same column (depth)
        Assert.NotEqual(b.Y, c.Y);            // Different rows
    }

    [Fact]
    public void AllDirections_ProducePositiveCoordinates()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B"), E("A", "C") };

        foreach (LayoutDirection dir in Enum.GetValues(typeof(LayoutDirection)))
        {
            var options = new GraphLayoutOptions { Direction = dir };
            var result = Layout(nodes, edges, options);

            foreach (var node in result.Nodes)
            {
                Assert.True(node.X >= 0, $"Node {node.Node.Id} has negative X in {dir}");
                Assert.True(node.Y >= 0, $"Node {node.Node.Id} has negative Y in {dir}");
            }
        }
    }

    [Fact]
    public void AllDirections_TotalDimensionsArePositive()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge> { E("A", "B") };

        foreach (LayoutDirection dir in Enum.GetValues(typeof(LayoutDirection)))
        {
            var options = new GraphLayoutOptions { Direction = dir };
            var result = Layout(nodes, edges, options);

            Assert.True(result.TotalWidth > 0, $"TotalWidth should be > 0 for {dir}");
            Assert.True(result.TotalHeight > 0, $"TotalHeight should be > 0 for {dir}");
        }
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "LayoutDirectionTests" -v minimal`
Expected: `BottomToTop`, `LeftToRight`, `RightToLeft` tests FAIL.

**Step 3: Add TransformForDirection to HierarchicalTreeLayout**

In `HierarchicalTreeLayout.Calculate()`, add a direction transform step right before computing total dimensions (before the `if (result.Nodes.Count > 0)` block near the end). Insert:

```csharp
// Apply direction transform
if (options.Direction != LayoutDirection.TopToBottom)
{
    TransformForDirection(result, options.Direction);
}
```

Add the `TransformForDirection` static method to the class:

```csharp
private static void TransformForDirection(LayoutResult result, LayoutDirection direction)
{
    // First compute the TB bounding box
    double maxX = result.Nodes.Max(n => n.X + n.Width);
    double maxY = result.Nodes.Max(n => n.Y + n.Height);

    switch (direction)
    {
        case LayoutDirection.BottomToTop:
            // Flip Y: y = maxY - y - height
            foreach (var node in result.Nodes)
                node.Y = maxY - node.Y - node.Height;
            break;

        case LayoutDirection.LeftToRight:
            // Swap axes: (x,y) → (y,x) and swap width/height per node
            foreach (var node in result.Nodes)
            {
                (node.X, node.Y) = (node.Y, node.X);
                (node.Width, node.Height) = (node.Height, node.Width);
            }
            break;

        case LayoutDirection.RightToLeft:
            // Swap axes + flip X
            double swappedMaxX = maxY; // After swap, the max X will be what was maxY
            foreach (var node in result.Nodes)
            {
                double newX = node.Y;
                double newY = node.X;
                (node.Width, node.Height) = (node.Height, node.Width);
                node.X = swappedMaxX - newX - node.Width;
                node.Y = newY;
            }
            break;
    }

    // Recompute total dimensions
    if (result.Nodes.Count > 0)
    {
        result.TotalWidth = result.Nodes.Max(n => n.X + n.Width);
        result.TotalHeight = result.Nodes.Max(n => n.Y + n.Height);
    }

    // Regenerate edge paths with the transformed positions
    // (edges were already built by BuildEdgePath which reads direction from options)
    // No action needed — edges are built AFTER this transform is called...
    // Wait — we need to restructure: transform nodes BEFORE building edges.
}
```

**IMPORTANT:** The Calculate method needs restructuring. The direction transform must happen AFTER node positioning but BEFORE edge path generation. Restructure `Calculate()` so the order is:

1. Position nodes (BFS depths + post-order X positioning) — always in TopToBottom
2. Apply `TransformForDirection` to node positions
3. Recompute `TotalWidth`/`TotalHeight`
4. Generate edge paths (BuildEdgePath already reads `options.Direction` for connection points)

The restructured tail of `Calculate()` should be:

```csharp
result.Nodes = positionedNodes.Values.ToList();

// Apply direction transform BEFORE edge generation
if (options.Direction != LayoutDirection.TopToBottom)
{
    TransformForDirection(result, options.Direction);
    // Update positionedNodes dict to reflect transformed positions
    positionedNodes = result.Nodes.ToDictionary(n => n.Node.Id);
}

// Generate edge paths (uses transformed positions + direction-aware connection points)
foreach (var edge in edges)
{
    if (positionedNodes.ContainsKey(edge.SourceNodeId) && positionedNodes.ContainsKey(edge.TargetNodeId))
    {
        var source = positionedNodes[edge.SourceNodeId];
        var target = positionedNodes[edge.TargetNodeId];
        var edgePath = BuildEdgePath(source, target, edge, options);
        result.Edges.Add(edgePath);
    }
}

// Compute total dimensions
if (result.Nodes.Count > 0)
{
    result.TotalWidth = result.Nodes.Max(n => n.X + n.Width);
    result.TotalHeight = result.Nodes.Max(n => n.Y + n.Height);
}

return result;
```

And simplify `TransformForDirection` to only transform node positions (remove the total dimensions recomputation and edge rebuilding — those happen after):

```csharp
private static void TransformForDirection(LayoutResult result, LayoutDirection direction)
{
    double maxX = result.Nodes.Max(n => n.X + n.Width);
    double maxY = result.Nodes.Max(n => n.Y + n.Height);

    switch (direction)
    {
        case LayoutDirection.BottomToTop:
            foreach (var node in result.Nodes)
                node.Y = maxY - node.Y - node.Height;
            break;

        case LayoutDirection.LeftToRight:
            foreach (var node in result.Nodes)
            {
                (node.X, node.Y) = (node.Y, node.X);
                (node.Width, node.Height) = (node.Height, node.Width);
            }
            break;

        case LayoutDirection.RightToLeft:
            double swappedMaxX = maxY;
            foreach (var node in result.Nodes)
            {
                double newX = node.Y;
                double newY = node.X;
                (node.Width, node.Height) = (node.Height, node.Width);
                node.X = swappedMaxX - newX - node.Width;
                node.Y = newY;
            }
            break;
    }
}
```

**Step 4: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj -v minimal`
Expected: ALL tests pass (new direction tests + existing tests).

**Step 5: Commit**

```bash
git add Library/GraphView/Layout/HierarchicalTreeLayout.cs Tests/Library/GraphView/LayoutDirectionTests.cs
git commit -m "feat(graph): add 4-way layout direction with coordinate transform"
```

---

## Task 4: Update ForestLayout to Pass Direction and Routing

**Files:**
- Modify: `Library/GraphView/Layout/ForestLayout.cs`
- Modify: `Tests/Library/GraphView/ForestLayoutTests.cs`

**Step 1: Add direction test to ForestLayoutTests.cs**

Add these tests to the existing `ForestLayoutTests` class:

```csharp
[Fact]
public void LeftToRight_ComponentsStackedVertically()
{
    var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
    var edges = new List<GraphEdge> { E("A", "B") }; // A-B is one tree, C is isolated

    var options = new GraphLayoutOptions { Direction = LayoutDirection.LeftToRight };
    var layout = new ForestLayout();
    var result = layout.Calculate(nodes, edges, options);

    // In LR mode, ForestLayout should stack components vertically (by Y offset)
    // rather than horizontally
    var ab = result.Nodes.Where(n => n.Node.Id == "A" || n.Node.Id == "B");
    var c = result.Nodes.First(n => n.Node.Id == "C");

    double abMaxY = ab.Max(n => n.Y + n.Height);
    Assert.True(c.Y >= abMaxY, "In LR mode, second component should be below first");
}

[Fact]
public void ForestLayout_PassesRoutingToDelegate()
{
    var nodes = new List<GraphNode> { N("A"), N("B") };
    var edges = new List<GraphEdge> { E("A", "B") };

    var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Straight };
    var layout = new ForestLayout();
    var result = layout.Calculate(nodes, edges, options);

    var path = result.Edges[0].SvgPath;
    Assert.Contains("L", path);
    Assert.DoesNotContain("C", path);
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/equiavia.components.Tests.csproj --filter "ForestLayoutTests" -v minimal`
Expected: `LeftToRight_ComponentsStackedVertically` FAILS (currently always offsets on X axis). `ForestLayout_PassesRoutingToDelegate` may pass if options are already passed through.

**Step 3: Update ForestLayout**

The key change: ForestLayout currently always offsets components along the X axis. For LR/RL directions, components should be offset along the Y axis instead.

Also, ForestLayout currently delegates to `HierarchicalTreeLayout` which now handles direction internally. So ForestLayout needs to:
1. Delegate to HierarchicalTreeLayout with the full options (direction + routing are already in `options`)
2. Choose the offset axis based on direction
3. Stop using `OffsetSvgPathX` (the delegated layout already generates correct edge paths with the right positions)

**The critical insight:** Since HierarchicalTreeLayout now transforms positions based on direction, ForestLayout should NOT apply its own direction logic — it should let the delegate handle it. But ForestLayout does need to offset components along the correct axis.

Replace the `Calculate` method's offset logic. The `foreach (var component in components)` loop should be:

```csharp
bool isVerticalFlow = options.Direction == LayoutDirection.TopToBottom
                   || options.Direction == LayoutDirection.BottomToTop;

foreach (var component in components)
{
    var componentNodeIds = new HashSet<string>(component.Select(n => n.Id));
    var componentEdges = edges
        .Where(e => componentNodeIds.Contains(e.SourceNodeId) && componentNodeIds.Contains(e.TargetNodeId))
        .ToList();

    var componentResult = hierarchicalLayout.Calculate(component, componentEdges, options);

    if (isVerticalFlow)
    {
        // Components arranged left-to-right (offset X)
        foreach (var pn in componentResult.Nodes)
        {
            pn.X += xOffset;
            result.Nodes.Add(pn);
        }

        foreach (var ep in componentResult.Edges)
        {
            if (xOffset > 0)
                ep.SvgPath = OffsetSvgPath(ep.SvgPath, xOffset, 0);
            ep.LabelX += xOffset;
            result.Edges.Add(ep);
        }

        double componentWidth = componentResult.TotalWidth;
        if (componentWidth <= 0 && componentResult.Nodes.Count > 0)
            componentWidth = componentResult.Nodes.Max(n => n.X + n.Width) - componentResult.Nodes.Min(n => n.X);
        xOffset += componentWidth + options.HorizontalSpacing;
    }
    else
    {
        // Components arranged top-to-bottom (offset Y) for LR/RL
        foreach (var pn in componentResult.Nodes)
        {
            pn.Y += yOffset;
            result.Nodes.Add(pn);
        }

        foreach (var ep in componentResult.Edges)
        {
            if (yOffset > 0)
                ep.SvgPath = OffsetSvgPath(ep.SvgPath, 0, yOffset);
            ep.LabelY += yOffset;
            result.Edges.Add(ep);
        }

        double componentHeight = componentResult.TotalHeight;
        if (componentHeight <= 0 && componentResult.Nodes.Count > 0)
            componentHeight = componentResult.Nodes.Max(n => n.Y + n.Height) - componentResult.Nodes.Min(n => n.Y);
        yOffset += componentHeight + options.VerticalSpacing;
    }
}
```

Add `double yOffset = 0;` alongside the existing `double xOffset = 0;`.

Rename `OffsetSvgPathX` to `OffsetSvgPath` and update it to handle both X and Y offsets:

```csharp
private static string OffsetSvgPath(string path, double offsetX, double offsetY)
{
    if (offsetX == 0 && offsetY == 0)
        return path;

    var parts = path.Split(' ');
    var result = new List<string>();

    foreach (var part in parts)
    {
        if (part.Contains(','))
        {
            var coords = part.Split(',');
            if (coords.Length == 2 &&
                double.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
            {
                result.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1}", x + offsetX, y + offsetY));
            }
            else
            {
                result.Add(part);
            }
        }
        else
        {
            result.Add(part);
        }
    }

    return string.Join(" ", result);
}
```

**Step 4: Run tests**

Run: `dotnet test Tests/equiavia.components.Tests.csproj -v minimal`
Expected: ALL tests pass.

**Step 5: Commit**

```bash
git add Library/GraphView/Layout/ForestLayout.cs Tests/Library/GraphView/ForestLayoutTests.cs
git commit -m "feat(graph): update ForestLayout for direction-aware component arrangement"
```
