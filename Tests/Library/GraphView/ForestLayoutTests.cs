using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class ForestLayoutTests
{
    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };

    private static LayoutResult Layout(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions? options = null)
    {
        var layout = new ForestLayout();
        return layout.Calculate(nodes, edges, options ?? new GraphLayoutOptions());
    }

    [Fact]
    public void TwoTrees_ArrangedSideBySide()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("A", "B"), E("C", "D") };

        var result = Layout(nodes, edges);

        var tree1Nodes = result.Nodes.Where(n => n.Node.Id == "A" || n.Node.Id == "B").ToList();
        var tree2Nodes = result.Nodes.Where(n => n.Node.Id == "C" || n.Node.Id == "D").ToList();

        double tree1MaxRight = tree1Nodes.Max(n => n.X + n.Width);
        double tree2MinLeft = tree2Nodes.Min(n => n.X);

        Assert.True(tree2MinLeft >= tree1MaxRight, $"Tree2 left ({tree2MinLeft}) should be >= Tree1 right ({tree1MaxRight})");
    }

    [Fact]
    public void TreesOrderedByDataAppearance()
    {
        var nodes = new List<GraphNode> { N("X"), N("Y"), N("A"), N("B") };
        var edges = new List<GraphEdge> { E("X", "Y"), E("A", "B") };

        var result = Layout(nodes, edges);

        var xNode = result.Nodes.First(n => n.Node.Id == "X");
        var aNode = result.Nodes.First(n => n.Node.Id == "A");

        Assert.True(xNode.X < aNode.X, "First root in data should be leftmost");
    }

    [Fact]
    public void SingleTree_DelegatesToHierarchical()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge> { E("A", "B") };

        var result = Layout(nodes, edges);

        Assert.Equal(2, result.Nodes.Count);
        Assert.Single(result.Edges);
    }

    [Fact]
    public void TotalDimensions_SpanAllTrees()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("A", "B"), E("C", "D") };

        var result = Layout(nodes, edges);

        Assert.True(result.TotalWidth > 0);
        Assert.True(result.TotalHeight > 0);
    }

    [Fact]
    public void DisconnectedSingleNodes_EachPositioned()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge>();

        var result = Layout(nodes, edges);

        Assert.Equal(3, result.Nodes.Count);
        var xValues = result.Nodes.Select(n => n.X).Distinct().ToList();
        Assert.Equal(3, xValues.Count);
    }

    [Fact]
    public void LeftToRight_ComponentsStackedVertically()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B") }; // A-B is one tree, C is isolated

        var options = new GraphLayoutOptions { Direction = LayoutDirection.LeftToRight };
        var result = Layout(nodes, edges, options);

        // In LR mode, ForestLayout should stack components vertically (by Y offset)
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
        var result = Layout(nodes, edges, options);

        var path = result.Edges[0].SvgPath;
        Assert.Contains("L", path);
        Assert.DoesNotContain("C", path);
    }
}
