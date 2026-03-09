using Bunit;
using equiavia.components.Library.GraphView;
using equiavia.components.Library.GraphView.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace equiavia.components.Tests.Library.GraphView;

public class EqGraphViewTests : BunitContext
{
    // GraphViewJSInterop only implements IAsyncDisposable; bUnit disposes services
    // synchronously, so we add a no-op IDisposable implementation for tests.
    private sealed class TestGraphJSInterop : GraphViewJSInterop, IDisposable
    {
        public TestGraphJSInterop(IJSRuntime jsRuntime) : base(jsRuntime) { }
        public void Dispose() { }
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    public EqGraphViewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/equiavia.components.Library/GraphViewJsInterop.js");
        Services.AddScoped<GraphViewJSInterop, TestGraphJSInterop>();
    }

    private static GraphData SimpleTree() => new()
    {
        Nodes = new List<GraphNode>
        {
            new GraphNode { Id = "1", Label = "Root" },
            new GraphNode { Id = "2", Label = "Child A" },
            new GraphNode { Id = "3", Label = "Child B" },
        },
        Edges = new List<GraphEdge>
        {
            new GraphEdge { Id = "e1", SourceNodeId = "1", TargetNodeId = "2" },
            new GraphEdge { Id = "e2", SourceNodeId = "1", TargetNodeId = "3" },
        }
    };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void EmptyState_ShowsMessage()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, (GraphData)null!)
            .Add(x => x.EmptyStateMessage, "Nothing here"));

        Assert.Contains("Nothing here", cut.Markup);
        Assert.Contains("eq-graph-empty", cut.Markup);
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
    public async Task SelectedNodeId_HighlightsNode()
    {
        var data = SimpleTree();
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, data));

        // Use InvokeAsync to run on the renderer's dispatcher
        await cut.InvokeAsync(() => cut.Instance.SetSelectedNode("2"));

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
        var nodeElement = cut.Find(".eq-graph-node");
        nodeElement.Click();

        Assert.NotNull(selected);
    }

    [Fact]
    public void EmptyData_NoEdgesRendered()
    {
        var emptyData = new GraphData
        {
            Nodes = new List<GraphNode>(),
            Edges = new List<GraphEdge>()
        };

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, emptyData));

        Assert.DoesNotContain("eq-graph-edge", cut.Markup);
    }

    [Fact]
    public void PerformanceMode_ShowsWarning()
    {
        var largeData = new GraphData();
        for (int i = 0; i < 510; i++)
        {
            largeData.Nodes.Add(new GraphNode { Id = $"n{i}", Label = $"Node {i}" });
            if (i > 0)
                largeData.Edges.Add(new GraphEdge
                {
                    Id = $"e{i}",
                    SourceNodeId = "n0",
                    TargetNodeId = $"n{i}"
                });
        }

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, largeData)
            .Add(x => x.PerformanceThreshold, 500));

        Assert.Contains("eq-graph-performance-warning", cut.Markup);
    }

    [Fact]
    public void MalformedData_EmitsWarning()
    {
        string? warningMessage = null;
        var badData = new GraphData
        {
            Nodes = new List<GraphNode>
            {
                new GraphNode { Id = "1", Label = "Solo" }
            },
            Edges = new List<GraphEdge>
            {
                new GraphEdge { Id = "orphan", SourceNodeId = "1", TargetNodeId = "missing" }
            }
        };

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, badData)
            .Add(x => x.OnDataWarning, (string msg) => { warningMessage = msg; }));

        Assert.NotNull(warningMessage);
        Assert.Contains("missing", warningMessage);
    }

    [Fact]
    public void CustomId_AppliedToContainer()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Id, "my-custom-graph"));

        Assert.Contains("my-custom-graph", cut.Markup);
    }

    [Fact]
    public void PerformanceWarning_HasAlertRole()
    {
        var largeData = new GraphData();
        for (int i = 0; i < 510; i++)
        {
            largeData.Nodes.Add(new GraphNode { Id = $"n{i}", Label = $"Node {i}" });
            if (i > 0)
                largeData.Edges.Add(new GraphEdge
                {
                    Id = $"e{i}",
                    SourceNodeId = "n0",
                    TargetNodeId = $"n{i}"
                });
        }

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, largeData)
            .Add(x => x.PerformanceThreshold, 500));

        var warning = cut.Find(".eq-graph-performance-warning");
        Assert.Equal("alert", warning.GetAttribute("role"));
    }

    [Fact]
    public void ContextAction_IncludesNodeInCallback()
    {
        (GraphNode Node, GraphContextAction Action)? result = null;
        var actions = new List<GraphContextAction>
        {
            new GraphContextAction { Id = "test", Label = "Test Action" }
        };

        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.ContextActions, actions)
            .Add(x => x.OnContextActionSelected, ((GraphNode Node, GraphContextAction Action) args) => { result = args; }));

        // Trigger context menu on a node
        var node = cut.Find(".eq-graph-node");
        node.ContextMenu();

        // Click the action in the context menu
        var menuItem = cut.Find(".eq-graph-context-item");
        menuItem.Click();

        Assert.NotNull(result);
        Assert.NotNull(result.Value.Node);
        Assert.Equal("test", result.Value.Action.Id);
    }

    [Fact]
    public void DirectionParameter_Accepted()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.Direction, LayoutDirection.LeftToRight));

        // Should render without errors
        Assert.Contains("eq-graph-node", cut.Markup);
    }

    [Fact]
    public void RoutingParameter_Accepted()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.Routing, EdgeRouting.Straight));

        Assert.Contains("eq-graph-edge", cut.Markup);
    }

    [Fact]
    public void CornerRadiusParameter_Accepted()
    {
        var cut = Render<EqGraphView>(p => p
            .Add(x => x.Data, SimpleTree())
            .Add(x => x.Routing, EdgeRouting.Orthogonal)
            .Add(x => x.CornerRadius, 12));

        Assert.Contains("eq-graph-edge", cut.Markup);
    }
}
