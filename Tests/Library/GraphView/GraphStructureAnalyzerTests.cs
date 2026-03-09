using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class GraphStructureAnalyzerTests
{
    private static GraphNode N(string id) => new() { Id = id, Label = id };
    private static GraphEdge E(string src, string tgt) => new() { Id = $"{src}-{tgt}", SourceNodeId = src, TargetNodeId = tgt };

    [Fact]
    public void SingleRoot_NoMultiParent_ReturnsHierarchicalTree()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "B"), E("A", "C") };

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.HierarchicalTree, result);
    }

    [Fact]
    public void MultipleRoots_NoMultiParent_ReturnsForest()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C"), N("D") };
        var edges = new List<GraphEdge> { E("A", "B"), E("C", "D") };

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.Forest, result);
    }

    [Fact]
    public void MultiParentNode_ReturnsDAG()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge> { E("A", "C"), E("B", "C") };

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.DAG, result);
    }

    [Fact]
    public void OnlyUndirectedEdges_ReturnsNetwork()
    {
        var nodes = new List<GraphNode> { N("A"), N("B") };
        var edges = new List<GraphEdge>
        {
            new() { Id = "e1", SourceNodeId = "A", TargetNodeId = "B", Direction = EdgeDirection.Undirected }
        };

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.Network, result);
    }

    [Fact]
    public void EmptyGraph_ReturnsForest()
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.Forest, result);
    }

    [Fact]
    public void SingleNode_NoEdges_ReturnsHierarchicalTree()
    {
        var nodes = new List<GraphNode> { N("A") };
        var edges = new List<GraphEdge>();

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.HierarchicalTree, result);
    }

    [Fact]
    public void DisconnectedNodes_ReturnsForest()
    {
        var nodes = new List<GraphNode> { N("A"), N("B"), N("C") };
        var edges = new List<GraphEdge>();

        var result = GraphStructureAnalyzer.Detect(nodes, edges);

        Assert.Equal(GraphLayoutMode.Forest, result);
    }
}
