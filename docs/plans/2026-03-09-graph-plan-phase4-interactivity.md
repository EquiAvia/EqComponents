# Phase 4: JS Interop & Interactivity

**Teammate:** Blazor Component Specialist + Engineer (Opus recommended — JS interop, platform concerns)
**Depends on:** Phase 3
**Verification:** `dotnet build equiavia.components.sln`

**Reference files:**
- Existing JS interop: `Library/TreeView/TreeViewJsInterop.cs`, `Library/wwwroot/TreeViewJsInterop.js`
- Design doc: `docs/plans/2026-03-09-graph-visualization-design.md` (Section 7)

---

### Task 4.1: Create GraphViewJsInterop.js

**Files:**
- Create: `Library/wwwroot/GraphViewJsInterop.js`

**Step 1: Write the JS module**

```javascript
// Graph viewport management — zoom, pan, long-press detection
// Uses pointer events (not mouse events) for MAUI Hybrid compatibility

let instances = {};

export function initialize(dotNetRef, svgId, viewportId, minZoom, maxZoom) {
    const svg = document.getElementById(svgId);
    const viewport = document.getElementById(viewportId);
    if (!svg || !viewport) return false;

    const state = {
        dotNetRef,
        svg,
        viewport,
        minZoom,
        maxZoom,
        x: 0,
        y: 0,
        scale: 1,
        isPanning: false,
        panStartX: 0,
        panStartY: 0,
        panStartTransX: 0,
        panStartTransY: 0,
        pointers: new Map(),
        initialPinchDistance: null,
        initialPinchScale: null,
        longPressTimer: null,
        longPressNodeId: null,
    };

    // Bind event handlers
    state.onPointerDown = (e) => handlePointerDown(state, e);
    state.onPointerMove = (e) => handlePointerMove(state, e);
    state.onPointerUp = (e) => handlePointerUp(state, e);
    state.onWheel = (e) => handleWheel(state, e);

    svg.addEventListener('pointerdown', state.onPointerDown);
    svg.addEventListener('pointermove', state.onPointerMove);
    svg.addEventListener('pointerup', state.onPointerUp);
    svg.addEventListener('pointercancel', state.onPointerUp);
    svg.addEventListener('wheel', state.onWheel, { passive: false });

    instances[svgId] = state;
    return true;
}

export function zoomToFit(svgId, contentWidth, contentHeight, padding) {
    const state = instances[svgId];
    if (!state) return;

    padding = padding || 40;
    const svgRect = state.svg.getBoundingClientRect();
    const availW = svgRect.width - padding * 2;
    const availH = svgRect.height - padding * 2;

    if (contentWidth <= 0 || contentHeight <= 0 || availW <= 0 || availH <= 0) return;

    const scale = Math.min(availW / contentWidth, availH / contentHeight, state.maxZoom);
    const clampedScale = Math.max(scale, state.minZoom);

    const x = (svgRect.width - contentWidth * clampedScale) / 2;
    const y = (svgRect.height - contentHeight * clampedScale) / 2;

    animateToTransform(state, x, y, clampedScale, 300);
}

export function resetView(svgId, contentWidth, contentHeight) {
    zoomToFit(svgId, contentWidth, contentHeight, 40);
}

export function scrollToNode(svgId, nodeElementId) {
    const state = instances[svgId];
    if (!state) return false;

    const node = document.getElementById(nodeElementId);
    if (!node) return false;

    // Get node position in SVG coordinates
    const bbox = node.getBBox();
    const svgRect = state.svg.getBoundingClientRect();
    const centerX = bbox.x + bbox.width / 2;
    const centerY = bbox.y + bbox.height / 2;

    // Calculate transform to center this node
    const x = svgRect.width / 2 - centerX * state.scale;
    const y = svgRect.height / 2 - centerY * state.scale;

    animateToTransform(state, x, y, state.scale, 300);
    return true;
}

export function dispose(svgId) {
    const state = instances[svgId];
    if (!state) return;

    state.svg.removeEventListener('pointerdown', state.onPointerDown);
    state.svg.removeEventListener('pointermove', state.onPointerMove);
    state.svg.removeEventListener('pointerup', state.onPointerUp);
    state.svg.removeEventListener('pointercancel', state.onPointerUp);
    state.svg.removeEventListener('wheel', state.onWheel);

    delete instances[svgId];
}

// --- Internal handlers ---

function handlePointerDown(state, e) {
    state.pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

    if (state.pointers.size === 1) {
        // Check if clicking on a node (for long-press)
        const nodeEl = findNodeElement(e.target);
        if (nodeEl && e.pointerType === 'touch') {
            const nodeId = extractNodeId(nodeEl.id);
            state.longPressNodeId = nodeId;
            state.longPressTimer = setTimeout(() => {
                if (state.longPressNodeId === nodeId) {
                    state.dotNetRef.invokeMethodAsync('HandleLongPress', nodeId, e.clientX, e.clientY);
                    state.longPressNodeId = null;
                }
            }, 500);
        }

        // Start panning if not on a node or using mouse
        if (!nodeEl || e.pointerType !== 'touch') {
            state.isPanning = true;
            state.panStartX = e.clientX;
            state.panStartY = e.clientY;
            state.panStartTransX = state.x;
            state.panStartTransY = state.y;
        }
    }

    if (state.pointers.size === 2) {
        // Start pinch zoom
        state.isPanning = false;
        cancelLongPress(state);
        const pts = Array.from(state.pointers.values());
        state.initialPinchDistance = distance(pts[0], pts[1]);
        state.initialPinchScale = state.scale;
    }
}

function handlePointerMove(state, e) {
    state.pointers.set(e.pointerId, { x: e.clientX, y: e.clientY });

    // Cancel long-press if moved too far
    if (state.longPressTimer) {
        const start = state.pointers.get(e.pointerId);
        if (start) {
            const dx = e.clientX - state.panStartX;
            const dy = e.clientY - state.panStartY;
            if (Math.sqrt(dx * dx + dy * dy) > 10) {
                cancelLongPress(state);
            }
        }
    }

    if (state.pointers.size === 2 && state.initialPinchDistance != null) {
        // Pinch zoom
        const pts = Array.from(state.pointers.values());
        const dist = distance(pts[0], pts[1]);
        const newScale = clampScale(state, state.initialPinchScale * (dist / state.initialPinchDistance));
        applyTransform(state, state.x, state.y, newScale);
    } else if (state.isPanning && state.pointers.size === 1) {
        // Pan
        const dx = e.clientX - state.panStartX;
        const dy = e.clientY - state.panStartY;
        applyTransform(state, state.panStartTransX + dx, state.panStartTransY + dy, state.scale);
    }
}

function handlePointerUp(state, e) {
    state.pointers.delete(e.pointerId);
    cancelLongPress(state);

    if (state.pointers.size < 2) {
        state.initialPinchDistance = null;
        state.initialPinchScale = null;
    }
    if (state.pointers.size === 0) {
        state.isPanning = false;
    }
}

function handleWheel(state, e) {
    e.preventDefault();
    const zoomFactor = e.deltaY < 0 ? 1.1 : 0.9;
    const newScale = clampScale(state, state.scale * zoomFactor);

    // Zoom toward cursor position
    const svgRect = state.svg.getBoundingClientRect();
    const cursorX = e.clientX - svgRect.left;
    const cursorY = e.clientY - svgRect.top;

    const ratio = newScale / state.scale;
    const newX = cursorX - (cursorX - state.x) * ratio;
    const newY = cursorY - (cursorY - state.y) * ratio;

    applyTransform(state, newX, newY, newScale);
}

// --- Transform helpers ---

function applyTransform(state, x, y, scale) {
    state.x = x;
    state.y = y;
    state.scale = scale;
    state.viewport.setAttribute('transform', `translate(${x},${y}) scale(${scale})`);
}

function animateToTransform(state, targetX, targetY, targetScale, durationMs) {
    const startX = state.x;
    const startY = state.y;
    const startScale = state.scale;
    const startTime = performance.now();

    function frame(now) {
        const elapsed = now - startTime;
        const t = Math.min(elapsed / durationMs, 1);
        const ease = t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2; // easeInOutQuad

        const x = startX + (targetX - startX) * ease;
        const y = startY + (targetY - startY) * ease;
        const s = startScale + (targetScale - startScale) * ease;

        applyTransform(state, x, y, s);

        if (t < 1) requestAnimationFrame(frame);
    }

    requestAnimationFrame(frame);
}

function clampScale(state, scale) {
    return Math.min(Math.max(scale, state.minZoom), state.maxZoom);
}

function distance(p1, p2) {
    return Math.sqrt((p1.x - p2.x) ** 2 + (p1.y - p2.y) ** 2);
}

function findNodeElement(el) {
    while (el && el !== document) {
        if (el.classList && el.classList.contains('eq-graph-node')) return el;
        el = el.parentElement;
    }
    return null;
}

function extractNodeId(elementId) {
    // Element ID format: "componentId-node-nodeId"
    const match = elementId.match(/-node-(.+)$/);
    return match ? match[1] : null;
}

function cancelLongPress(state) {
    if (state.longPressTimer) {
        clearTimeout(state.longPressTimer);
        state.longPressTimer = null;
        state.longPressNodeId = null;
    }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded (JS is just a static asset)

**Step 3: Commit**

```bash
git add Library/wwwroot/GraphViewJsInterop.js
git commit -m "feat(graph): add GraphViewJsInterop.js for zoom, pan, pinch, long-press"
```

---

### Task 4.2: Create GraphViewJSInterop C# wrapper

**Files:**
- Create: `Library/GraphView/GraphViewJsInterop.cs`

**Step 1: Write the C# wrapper**

Follow the exact pattern from `Library/TreeView/TreeViewJsInterop.cs`:

```csharp
using Microsoft.JSInterop;

namespace equiavia.components.Library.GraphView
{
    public class GraphViewJSInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
        private DotNetObjectReference<GraphViewJSInterop>? _dotNetRef;
        private string? _svgId;

        // Callback for long-press — set by the component
        internal Func<string, double, double, Task>? OnLongPress { get; set; }

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

        [JSInvokable]
        public async Task HandleLongPress(string nodeId, double clientX, double clientY)
        {
            if (OnLongPress != null)
                await OnLongPress(nodeId, clientX, clientY);
        }

        public async ValueTask DisposeAsync()
        {
            if (_svgId != null && _moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.InvokeVoidAsync("dispose", _svgId);
                await module.DisposeAsync();
            }
            _dotNetRef?.Dispose();
        }
    }
}
```

**Step 2: Build**

Run: `dotnet build Library/equiavia.components.Library.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add Library/GraphView/GraphViewJsInterop.cs
git commit -m "feat(graph): add GraphViewJSInterop C# wrapper"
```

---

### Task 4.3: Register DI and wire JS interop into EqGraphView

**Files:**
- Modify: `Library/eqComponents.cs` — add `GraphViewJSInterop` registration
- Modify: `Library/GraphView/EqGraphView.Razor.cs` — add JS interop calls

**Step 1: Update DI registration**

In `Library/eqComponents.cs`, add the graph JS interop:

```csharp
using equiavia.components.Library.GraphView;
// ...
services.AddScoped<GraphViewJSInterop, GraphViewJSInterop>();
```

**Step 2: Wire JS interop into EqGraphView.Razor.cs**

Add to the component class:

```csharp
[Inject] public GraphViewJSInterop JsInterop { get; set; } = default!;
private bool _jsInitialized = false;
```

Add `OnAfterRenderAsync`:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (_layoutResult != null && !_jsInitialized)
    {
        _jsInitialized = true;
        JsInterop.OnLongPress = HandleLongPressFromJs;
        await JsInterop.Initialize($"{Id}-svg", $"{Id}-viewport", MinZoom, MaxZoom);
        await JsInterop.ZoomToFit(_layoutResult.TotalWidth, _layoutResult.TotalHeight);
    }
}
```

Add the long-press handler:

```csharp
private async Task HandleLongPressFromJs(string nodeId, double clientX, double clientY)
{
    var node = _sanitizedData?.Nodes.FirstOrDefault(n => n.Id == nodeId);
    if (node != null && ContextActions.Count > 0)
    {
        _contextMenuNode = node;
        _contextMenuX = clientX;
        _contextMenuY = clientY;
        _contextMenuVisible = true;
        await InvokeAsync(StateHasChanged);
    }
}
```

Update `ResetView()`:

```csharp
public async void ResetView()
{
    if (_layoutResult != null)
        await JsInterop.ZoomToFit(_layoutResult.TotalWidth, _layoutResult.TotalHeight);
}
```

Update `SetSelectedNode` to scroll:

```csharp
public async Task SetSelectedNode(string nodeId)
{
    UpdateSelection(nodeId);
    StateHasChanged();
    await JsInterop.ScrollToNode($"{Id}-node-{nodeId}");
}
```

Update `DisposeAsync`:

```csharp
public async ValueTask DisposeAsync()
{
    await JsInterop.DisposeAsync();
}
```

**Step 3: Build**

Run: `dotnet build equiavia.components.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/eqComponents.cs Library/GraphView/EqGraphView.Razor.cs
git commit -m "feat(graph): wire JS interop for zoom/pan/long-press into EqGraphView"
```

---

### Task 4.4: Wire context menu positioning from right-click

**Files:**
- Modify: `Library/GraphView/EqGraphView.Razor.cs`
- Modify: `Library/GraphView/EqGraphNode.razor`

**Step 1: Update EqGraphNode to pass MouseEventArgs**

Change `OnContextMenu` parameter from `EventCallback<GraphNode>` to `EventCallback<(GraphNode, Microsoft.AspNetCore.Components.Web.MouseEventArgs)>`:

In `EqGraphNode.razor`, change the `@oncontextmenu` handler to pass both:

```razor
@oncontextmenu="HandleContextMenu"
```

Add code:

```csharp
[Parameter] public EventCallback<(GraphNode Node, MouseEventArgs Args)> OnContextMenu { get; set; }

private async Task HandleContextMenu(MouseEventArgs args)
{
    await OnContextMenu.InvokeAsync((PositionedNode.Node, args));
}
```

**Step 2: Update EqGraphView to use mouse coordinates**

In `EqGraphView.Razor.cs`, update `HandleNodeContextMenu`:

```csharp
private async Task HandleNodeContextMenu((GraphNode Node, MouseEventArgs Args) args)
{
    _contextMenuNode = args.Node;
    _contextMenuX = args.Args.ClientX;
    _contextMenuY = args.Args.ClientY;
    _contextMenuVisible = true;
}
```

Update the markup in `EqGraphView.razor` to pass the new callback:

```razor
OnContextMenu="HandleNodeContextMenu"
```

**Step 3: Build**

Run: `dotnet build equiavia.components.sln`
Expected: Build succeeded

**Step 4: Commit**

```bash
git add Library/GraphView/EqGraphView.Razor.cs Library/GraphView/EqGraphView.razor Library/GraphView/EqGraphNode.razor
git commit -m "feat(graph): wire context menu positioning with client coordinates"
```

---

## Phase 4 Complete Checklist

After all tasks, verify:
- [ ] `dotnet build equiavia.components.sln` succeeds
- [ ] `Library/wwwroot/GraphViewJsInterop.js` exists
- [ ] `Library/GraphView/GraphViewJsInterop.cs` exists
- [ ] `Library/eqComponents.cs` registers `GraphViewJSInterop`
- [ ] `EqGraphView` initializes JS interop on first render
- [ ] Context menu opens on right-click and long-press
- [ ] All existing tests still pass
