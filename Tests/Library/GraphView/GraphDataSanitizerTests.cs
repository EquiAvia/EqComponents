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
                new GraphEdge { Id = "e3", SourceNodeId = "C", TargetNodeId = "A" }
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
