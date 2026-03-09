using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace equiavia.components.Library.GraphView
{
    public class GraphViewJSInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
        private DotNetObjectReference<GraphViewJSInterop> _dotNetRef;
        private string _svgId;

        internal Func<string, double, double, Task> OnLongPress { get; set; }

        public GraphViewJSInterop(IJSRuntime jsRuntime)
        {
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/equiavia.components.Library/GraphViewJsInterop.js").AsTask());
        }

        public async Task Initialize(string svgId, string viewportId, double minZoom, double maxZoom)
        {
            _svgId = svgId;
            _dotNetRef = DotNetObjectReference.Create(this);
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("initialize", _dotNetRef, svgId, viewportId, minZoom, maxZoom);
        }

        public async Task ZoomToFit(double contentWidth, double contentHeight, double padding = 40)
        {
            if (_svgId == null) return;
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("zoomToFit", _svgId, contentWidth, contentHeight, padding);
        }

        public async Task ResetView(double contentWidth, double contentHeight)
        {
            if (_svgId == null) return;
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("resetView", _svgId, contentWidth, contentHeight);
        }

        public async Task<bool> ScrollToNode(string nodeElementId)
        {
            if (_svgId == null) return false;
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<bool>("scrollToNode", _svgId, nodeElementId);
        }

        public async Task ZoomToNode(string nodeElementId, double targetScale = 1.5)
        {
            if (_svgId == null) return;
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("zoomToNode", _svgId, nodeElementId, targetScale);
        }

        [JSInvokable]
        public async Task HandleLongPress(string nodeId, double clientX, double clientY)
        {
            if (OnLongPress != null)
            {
                await OnLongPress(nodeId, clientX, clientY);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_svgId != null && _moduleTask.IsValueCreated)
            {
                try
                {
                    var module = await _moduleTask.Value;
                    await module.InvokeVoidAsync("dispose", _svgId);
                    await module.DisposeAsync();
                }
                catch (JSDisconnectedException)
                {
                    // Circuit disconnected, safe to ignore
                }
            }

            _dotNetRef?.Dispose();
        }
    }
}
