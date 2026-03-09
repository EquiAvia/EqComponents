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
        // Use 3 nodes so children are offset horizontally from parent, producing corners
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Orthogonal, CornerRadius = 8 };
        var result = Layout(new() { N("A"), N("B"), N("C") }, new() { E("A", "B"), E("A", "C") }, options);

        // At least one edge should have an arc (the ones where parent and child aren't vertically aligned)
        Assert.True(result.Edges.Any(e => e.SvgPath.Contains("A")), "At least one edge should contain arc command");
    }

    [Fact]
    public void OrthogonalRouting_ZeroCornerRadius_NoArcCommand()
    {
        var options = new GraphLayoutOptions { EdgeRouting = EdgeRouting.Orthogonal, CornerRadius = 0 };
        var result = Layout(new() { N("A"), N("B"), N("C") }, new() { E("A", "B"), E("A", "C") }, options);

        foreach (var edge in result.Edges)
        {
            Assert.DoesNotContain("A", edge.SvgPath); // No arc, just line segments
        }
    }
}
