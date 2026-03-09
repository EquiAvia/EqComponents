using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using equiavia.components.Library.GraphView.Layout;
using equiavia.components.Library.GraphView.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace equiavia.components.Library.GraphView
{
    public partial class EqGraphView : ComponentBase, IAsyncDisposable
    {
        [Inject] public GraphViewJSInterop JsInterop { get; set; } = default!;

        #region Parameters

        [Parameter] public GraphData Data { get; set; }
        [Parameter] public GraphLayoutMode LayoutMode { get; set; } = GraphLayoutMode.Auto;
        [Parameter] public string SelectedNodeId { get; set; }
        [Parameter] public List<GraphContextAction> ContextActions { get; set; }
        [Parameter] public double MinZoom { get; set; } = 0.1;
        [Parameter] public double MaxZoom { get; set; } = 3.0;
        [Parameter] public double InitialZoom { get; set; } = 1.0;
        [Parameter] public int PerformanceThreshold { get; set; } = 500;
        [Parameter] public bool IsLoading { get; set; }
        [Parameter] public string EmptyStateMessage { get; set; } = "No graph data to display.";
        [Parameter] public string Id { get; set; } = "eq-graph-view";
        [Parameter] public string CSSClasses { get; set; } = string.Empty;
        [Parameter] public string AdditionalStyles { get; set; } = string.Empty;
        [Parameter] public string Height { get; set; } = "500px";

        #endregion

        #region EventCallbacks

        [Parameter] public EventCallback<GraphNode> OnNodeSelected { get; set; }
        [Parameter] public EventCallback<GraphContextAction> OnContextActionSelected { get; set; }
        [Parameter] public EventCallback<GraphNode> OnBreadcrumbNavigated { get; set; }
        [Parameter] public EventCallback OnSelectionCleared { get; set; }
        [Parameter] public EventCallback<string> OnDataWarning { get; set; }

        #endregion

        #region Internal State

        private LayoutResult _layoutResult;
        private GraphLayoutMode _resolvedLayoutMode;
        private bool _isPerformanceMode;
        private string _activeSelectedNodeId;
        private string _focusedNodeId;
        private GraphData _sanitizedData;
        private GraphData _previousData;
        private Dictionary<string, NodeShape> _resolvedShapes = new();
        private string _currentRootNodeId;
        private List<GraphNode> _breadcrumbPath = new();
        private bool _contextMenuVisible;
        private double _contextMenuX;
        private double _contextMenuY;
        private GraphNode _contextMenuNode;
        private bool _jsInitialized;

        #endregion

        #region Lifecycle

        protected override async Task OnParametersSetAsync()
        {
            if (!ReferenceEquals(Data, _previousData))
            {
                _previousData = Data;
                _jsInitialized = false;
                await RunFullPipeline();
            }
            else if (SelectedNodeId != _activeSelectedNodeId)
            {
                UpdateSelection(SelectedNodeId);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_layoutResult != null && !_jsInitialized)
            {
                _jsInitialized = true;
                JsInterop.OnLongPress = HandleLongPressFromJs;
                try
                {
                    await JsInterop.Initialize(Id, $"{Id}-viewport", MinZoom, MaxZoom);
                    await JsInterop.ZoomToFit(_layoutResult.TotalWidth, _layoutResult.TotalHeight);
                }
                catch (JSDisconnectedException)
                {
                    // Circuit disconnected, safe to ignore
                }
            }
        }

        #endregion

        #region Public API

        public async Task SetSelectedNode(string nodeId)
        {
            UpdateSelection(nodeId);
            StateHasChanged();
            if (_jsInitialized && !string.IsNullOrEmpty(nodeId))
            {
                try
                {
                    await JsInterop.ScrollToNode($"{Id}-node-{nodeId}");
                }
                catch (JSDisconnectedException)
                {
                    // Circuit disconnected, safe to ignore
                }
            }
        }

        public async Task Refresh(GraphData data)
        {
            Data = data;
            _previousData = data;
            _jsInitialized = false;
            await RunFullPipeline();
            StateHasChanged();
        }

        public async Task ResetView()
        {
            if (_layoutResult != null && _jsInitialized)
            {
                await JsInterop.ZoomToFit(_layoutResult.TotalWidth, _layoutResult.TotalHeight);
            }
        }

        public async Task ExpandAll()
        {
            _currentRootNodeId = null;
            _breadcrumbPath.Clear();
            await RunLayout();
            StateHasChanged();
        }

        public async Task CollapseAll()
        {
            _currentRootNodeId = null;
            _breadcrumbPath.Clear();
            await RunLayout();
            StateHasChanged();
        }

        public async Task NavigateToRoot()
        {
            _currentRootNodeId = null;
            _breadcrumbPath.Clear();
            await RunLayout();
            StateHasChanged();
        }

        #endregion

        #region Data Processing Pipeline

        private async Task RunFullPipeline()
        {
            if (Data == null)
            {
                _layoutResult = null;
                _sanitizedData = null;
                _resolvedShapes.Clear();
                return;
            }

            // 1. Sanitize
            _sanitizedData = GraphDataSanitizer.Sanitize(Data, warning => EmitWarning(warning));

            // 2. Analyze structure
            var detectedMode = GraphStructureAnalyzer.Detect(_sanitizedData.Nodes, _sanitizedData.Edges);

            // 3. Resolve layout mode
            if (LayoutMode == GraphLayoutMode.Auto)
            {
                _resolvedLayoutMode = detectedMode;
            }
            else
            {
                _resolvedLayoutMode = LayoutMode;
                if (LayoutMode != detectedMode && detectedMode != GraphLayoutMode.Forest)
                {
                    EmitWarning($"Forced layout mode '{LayoutMode}' may not match detected structure '{detectedMode}'.");
                }
            }

            // 4. Resolve shapes
            ResolveShapes();

            // 5. Check performance threshold
            _isPerformanceMode = _sanitizedData.Nodes.Count > PerformanceThreshold;

            // 6. DAG/Network fallback
            if (_resolvedLayoutMode == GraphLayoutMode.DAG || _resolvedLayoutMode == GraphLayoutMode.Network)
            {
                EmitWarning($"Layout mode '{_resolvedLayoutMode}' is not supported in v1. Falling back to Forest layout.");
                _resolvedLayoutMode = GraphLayoutMode.Forest;
            }

            // 7. Run layout
            await RunLayout();

            // 8. Check stale selection
            CheckStaleSelection();
        }

        private Task RunLayout()
        {
            if (_sanitizedData == null || _sanitizedData.Nodes.Count == 0)
            {
                _layoutResult = null;
                return Task.CompletedTask;
            }

            var options = new GraphLayoutOptions
            {
                IsPerformanceMode = _isPerformanceMode
            };

            var nodes = _sanitizedData.Nodes;
            var edges = _sanitizedData.Edges;

            // If navigated into a subtree, filter to subtree
            if (!string.IsNullOrEmpty(_currentRootNodeId))
            {
                var subtreeIds = GetSubtreeNodeIds(_currentRootNodeId);
                nodes = nodes.Where(n => subtreeIds.Contains(n.Id)).ToList();
                edges = edges.Where(e => subtreeIds.Contains(e.SourceNodeId) && subtreeIds.Contains(e.TargetNodeId)).ToList();
            }

            IGraphLayout layout = _resolvedLayoutMode switch
            {
                GraphLayoutMode.HierarchicalTree => new HierarchicalTreeLayout(),
                _ => new ForestLayout()
            };

            _layoutResult = layout.Calculate(nodes, edges, options);

            return Task.CompletedTask;
        }

        private void ResolveShapes()
        {
            _resolvedShapes.Clear();
            bool isHierarchical = _resolvedLayoutMode == GraphLayoutMode.HierarchicalTree
                               || _resolvedLayoutMode == GraphLayoutMode.Forest;

            foreach (var node in _sanitizedData.Nodes)
            {
                if (node.Shape == NodeShape.Auto)
                {
                    _resolvedShapes[node.Id] = isHierarchical ? NodeShape.RoundedRectangle : NodeShape.Circle;
                }
                else
                {
                    _resolvedShapes[node.Id] = node.Shape;
                }
            }
        }

        private void CheckStaleSelection()
        {
            if (!string.IsNullOrEmpty(_activeSelectedNodeId)
                && _sanitizedData != null
                && !_sanitizedData.Nodes.Any(n => n.Id == _activeSelectedNodeId))
            {
                _activeSelectedNodeId = null;
                _focusedNodeId = null;
                _ = OnSelectionCleared.InvokeAsync();
            }
        }

        #endregion

        #region Event Handlers

        private async Task HandleNodeClick(GraphNode node)
        {
            UpdateSelection(node.Id);
            await OnNodeSelected.InvokeAsync(node);
        }

        private void HandleNodeContextMenu((GraphNode Node, MouseEventArgs Args) tuple)
        {
            _contextMenuNode = tuple.Node;
            _contextMenuX = tuple.Args.ClientX;
            _contextMenuY = tuple.Args.ClientY;
            _contextMenuVisible = true;
        }

        private async Task HandleContextAction(GraphContextAction action)
        {
            _contextMenuVisible = false;
            await OnContextActionSelected.InvokeAsync(action);
        }

        private void DismissContextMenu()
        {
            _contextMenuVisible = false;
        }

        private async Task HandleBreadcrumbNavigate(GraphNode node)
        {
            if (node == null)
            {
                _currentRootNodeId = null;
                _breadcrumbPath.Clear();
            }
            else
            {
                _currentRootNodeId = node.Id;
                var index = _breadcrumbPath.FindIndex(n => n.Id == node.Id);
                if (index >= 0)
                {
                    _breadcrumbPath = _breadcrumbPath.Take(index + 1).ToList();
                }
            }

            await RunLayout();
            await OnBreadcrumbNavigated.InvokeAsync(node);
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case "Enter":
                    if (!string.IsNullOrEmpty(_focusedNodeId) && _sanitizedData != null)
                    {
                        var node = _sanitizedData.Nodes.FirstOrDefault(n => n.Id == _focusedNodeId);
                        if (node != null)
                        {
                            UpdateSelection(node.Id);
                            await OnNodeSelected.InvokeAsync(node);
                        }
                    }
                    break;

                case " ":
                    if (!string.IsNullOrEmpty(_focusedNodeId) && _sanitizedData != null)
                    {
                        var node = _sanitizedData.Nodes.FirstOrDefault(n => n.Id == _focusedNodeId);
                        if (node != null)
                        {
                            _contextMenuNode = node;
                            _contextMenuVisible = true;
                        }
                    }
                    break;

                case "Escape":
                    if (_contextMenuVisible)
                    {
                        DismissContextMenu();
                    }
                    else if (!string.IsNullOrEmpty(_currentRootNodeId))
                    {
                        await HandleBreadcrumbNavigate(
                            _breadcrumbPath.Count > 1
                                ? _breadcrumbPath[_breadcrumbPath.Count - 2]
                                : null);
                    }
                    break;
            }
        }

        private async Task HandleLongPressFromJs(string nodeId, double clientX, double clientY)
        {
            if (_sanitizedData == null) return;
            var node = _sanitizedData.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null) return;

            _contextMenuNode = node;
            _contextMenuX = clientX;
            _contextMenuY = clientY;
            _contextMenuVisible = true;
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region Helpers

        private void UpdateSelection(string nodeId)
        {
            _activeSelectedNodeId = nodeId;
            _focusedNodeId = nodeId;
        }

        private HashSet<string> GetSubtreeNodeIds(string rootId)
        {
            var result = new HashSet<string>();
            if (_sanitizedData == null) return result;

            var childrenMap = new Dictionary<string, List<string>>();
            foreach (var node in _sanitizedData.Nodes)
                childrenMap[node.Id] = new List<string>();

            foreach (var edge in _sanitizedData.Edges)
            {
                if (childrenMap.ContainsKey(edge.SourceNodeId))
                    childrenMap[edge.SourceNodeId].Add(edge.TargetNodeId);
            }

            var queue = new Queue<string>();
            queue.Enqueue(rootId);
            result.Add(rootId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (childrenMap.TryGetValue(current, out var children))
                {
                    foreach (var childId in children)
                    {
                        if (result.Add(childId))
                        {
                            queue.Enqueue(childId);
                        }
                    }
                }
            }

            return result;
        }

        private NodeShape GetResolvedShape(GraphNode node)
        {
            return _resolvedShapes.TryGetValue(node.Id, out var shape) ? shape : NodeShape.RoundedRectangle;
        }

        private void EmitWarning(string message)
        {
            if (OnDataWarning.HasDelegate)
            {
                _ = OnDataWarning.InvokeAsync(message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_jsInitialized)
            {
                try
                {
                    await JsInterop.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // Circuit disconnected, safe to ignore
                }
            }
        }

        #endregion
    }
}
