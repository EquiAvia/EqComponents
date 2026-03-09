using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class HierarchicalTreeLayoutTests
{
    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };

    private static LayoutResult Layout(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions? options = null)
    {
        var layout = new HierarchicalTreeLayout();
        return layout.Calculate(nodes, edges, options ?? new GraphLayoutOptions());
    }

    [Fact]
    public void SingleNode_PositionedAtOrigin()
    {
        var result = Layout(new() { N("A") }, new());

        Assert.Single(result.Nodes);
        Assert.Equal(0, result.Nodes[0].Y);
    }

    [Fact]
    public void ParentChild_ChildBelowParent()
    {
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") });

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var child = result.Nodes.First(n => n.Node.Id == "B");

        Assert.True(child.Y > parent.Y);
    }

    [Fact]
    public void TwoChildren_SideBySide()
    {
        var result = Layout(new() { N("A"), N("B"), N("C") }, new() { E("A", "B"), E("A", "C") });

        var b = result.Nodes.First(n => n.Node.Id == "B");
        var c = result.Nodes.First(n => n.Node.Id == "C");

        Assert.NotEqual(b.X, c.X);
        Assert.Equal(b.Y, c.Y);
    }

    [Fact]
    public void ParentCenteredOverChildren()
    {
        var result = Layout(new() { N("A"), N("B"), N("C") }, new() { E("A", "B"), E("A", "C") });

        var parent = result.Nodes.First(n => n.Node.Id == "A");
        var b = result.Nodes.First(n => n.Node.Id == "B");
        var c = result.Nodes.First(n => n.Node.Id == "C");

        double expectedX = (b.X + b.Width / 2 + c.X + c.Width / 2) / 2 - parent.Width / 2;
        Assert.Equal(expectedX, parent.X, precision: 1);
    }

    [Fact]
    public void EdgesGenerated_WithValidSvgPaths()
    {
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") });

        Assert.Single(result.Edges);
        var path = result.Edges[0].SvgPath;
        Assert.StartsWith("M", path);
        Assert.Contains("C", path);
    }

    [Fact]
    public void NoOverlap_BetweenSubtrees()
    {
        var nodes = new List<GraphNode> { N("R"), N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("R", "A"), E("R", "B"), E("A", "C"), E("B", "D") };
        var options = new GraphLayoutOptions();
        var result = Layout(nodes, edges, options);

        // Group by Y level
        var levels = result.Nodes.GroupBy(n => n.Y);
        foreach (var level in levels)
        {
            var sorted = level.OrderBy(n => n.X).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                double gap = sorted[i].X - (sorted[i - 1].X + sorted[i - 1].Width);
                Assert.True(gap >= 0, $"Overlap detected at level Y={level.Key}: node {sorted[i].Node.Id} overlaps {sorted[i - 1].Node.Id}");
            }
        }
    }

    [Fact]
    public void TotalDimensions_ArePositive()
    {
        var result = Layout(new() { N("A"), N("B") }, new() { E("A", "B") });

        Assert.True(result.TotalWidth > 0);
        Assert.True(result.TotalHeight > 0);
    }

    [Fact]
    public void NodeDimensions_ReflectShape()
    {
        var circle = new GraphNode { Id = "C", Label = "C", Shape = NodeShape.Circle };
        var rect = new GraphNode { Id = "R", Label = "R", Shape = NodeShape.Rectangle };

        var result = Layout(new() { circle, rect }, new() { E("C", "R") });

        var cNode = result.Nodes.First(n => n.Node.Id == "C");
        var rNode = result.Nodes.First(n => n.Node.Id == "R");

        Assert.Equal(cNode.Width, cNode.Height);
        Assert.True(rNode.Width > rNode.Height);
    }
}
