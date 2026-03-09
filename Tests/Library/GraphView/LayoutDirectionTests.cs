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
