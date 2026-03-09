// GraphView JS Interop — zoom, pan, pinch, long-press support
// Keyed per SVG instance to support multiple graph views on one page.

const instances = new Map();

function easeInOutQuad(t) {
    return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
}

function findNodeElement(target) {
    let el = target;
    while (el && el !== document) {
        if (el.classList && el.classList.contains('eq-graph-node')) {
            return el;
        }
        el = el.parentElement;
    }
    return null;
}

function extractNodeId(elementId, componentId) {
    const prefix = componentId + '-node-';
    if (elementId && elementId.startsWith(prefix)) {
        return elementId.substring(prefix.length);
    }
    return null;
}

function applyTransform(state) {
    if (state.viewportEl) {
        state.viewportEl.setAttribute('transform',
            `translate(${state.transform.x},${state.transform.y}) scale(${state.transform.scale})`);
    }
}

function animateToTransform(state, targetX, targetY, targetScale, duration) {
    duration = duration || 300;
    const startX = state.transform.x;
    const startY = state.transform.y;
    const startScale = state.transform.scale;
    const startTime = performance.now();

    if (state.animationFrame) {
        cancelAnimationFrame(state.animationFrame);
    }

    function step(now) {
        const elapsed = now - startTime;
        const t = Math.min(elapsed / duration, 1);
        const e = easeInOutQuad(t);

        state.transform.x = startX + (targetX - startX) * e;
        state.transform.y = startY + (targetY - startY) * e;
        state.transform.scale = startScale + (targetScale - startScale) * e;
        applyTransform(state);

        if (t < 1) {
            state.animationFrame = requestAnimationFrame(step);
        } else {
            state.animationFrame = null;
        }
    }

    state.animationFrame = requestAnimationFrame(step);
}

function getPointerDistance(pointers) {
    const keys = Array.from(pointers.keys());
    if (keys.length < 2) return 0;
    const p1 = pointers.get(keys[0]);
    const p2 = pointers.get(keys[1]);
    const dx = p1.clientX - p2.clientX;
    const dy = p1.clientY - p2.clientY;
    return Math.sqrt(dx * dx + dy * dy);
}

function getPointerCenter(pointers) {
    const keys = Array.from(pointers.keys());
    if (keys.length < 2) return { x: 0, y: 0 };
    const p1 = pointers.get(keys[0]);
    const p2 = pointers.get(keys[1]);
    return {
        x: (p1.clientX + p2.clientX) / 2,
        y: (p1.clientY + p2.clientY) / 2
    };
}

export function initialize(dotNetRef, svgId, viewportId, minZoom, maxZoom) {
    const svgEl = document.getElementById(svgId)?.querySelector('.eq-graph-svg');
    const viewportEl = document.getElementById(viewportId);

    if (!svgEl || !viewportEl) {
        console.warn('GraphViewJsInterop: SVG or viewport element not found', svgId, viewportId);
        return;
    }

    const state = {
        dotNetRef,
        svgEl,
        viewportEl,
        svgId,
        componentId: svgId,
        minZoom: minZoom || 0.1,
        maxZoom: maxZoom || 3.0,
        transform: { x: 0, y: 0, scale: 1 },
        isPanning: false,
        panStart: { x: 0, y: 0 },
        panTransformStart: { x: 0, y: 0 },
        pointers: new Map(),
        pinchStartDist: 0,
        pinchStartScale: 1,
        longPressTimer: null,
        longPressStartPos: null,
        animationFrame: null,
        handlers: {}
    };

    // Pointer event handlers
    state.handlers.pointerdown = function (e) {
        state.pointers.set(e.pointerId, { clientX: e.clientX, clientY: e.clientY });

        if (state.pointers.size === 2) {
            // Start pinch
            state.isPanning = false;
            state.pinchStartDist = getPointerDistance(state.pointers);
            state.pinchStartScale = state.transform.scale;
            clearLongPress(state);
            return;
        }

        if (state.pointers.size === 1) {
            // Check for long-press on a node (touch)
            if (e.pointerType === 'touch') {
                const nodeEl = findNodeElement(e.target);
                if (nodeEl) {
                    state.longPressStartPos = { x: e.clientX, y: e.clientY };
                    state.longPressTimer = setTimeout(function () {
                        const nodeId = extractNodeId(nodeEl.id, state.componentId);
                        if (nodeId && state.dotNetRef) {
                            state.dotNetRef.invokeMethodAsync('HandleLongPress', nodeId, e.clientX, e.clientY);
                        }
                        state.longPressTimer = null;
                    }, 500);
                }
            }

            // Check if clicking on SVG background (not on a node) to start pan
            const nodeEl = findNodeElement(e.target);
            if (!nodeEl) {
                state.isPanning = true;
                state.panStart = { x: e.clientX, y: e.clientY };
                state.panTransformStart = { x: state.transform.x, y: state.transform.y };
                svgEl.style.cursor = 'grabbing';
            }
        }
    };

    state.handlers.pointermove = function (e) {
        if (state.pointers.has(e.pointerId)) {
            state.pointers.set(e.pointerId, { clientX: e.clientX, clientY: e.clientY });
        }

        // Cancel long-press if moved too far
        if (state.longPressTimer && state.longPressStartPos) {
            const dx = e.clientX - state.longPressStartPos.x;
            const dy = e.clientY - state.longPressStartPos.y;
            if (Math.sqrt(dx * dx + dy * dy) > 10) {
                clearLongPress(state);
            }
        }

        // Pinch zoom
        if (state.pointers.size === 2) {
            const dist = getPointerDistance(state.pointers);
            if (state.pinchStartDist > 0) {
                const ratio = dist / state.pinchStartDist;
                let newScale = state.pinchStartScale * ratio;
                newScale = Math.max(state.minZoom, Math.min(state.maxZoom, newScale));

                const center = getPointerCenter(state.pointers);
                const rect = svgEl.getBoundingClientRect();
                const cx = center.x - rect.left;
                const cy = center.y - rect.top;

                const scaleDelta = newScale / state.transform.scale;
                state.transform.x = cx - scaleDelta * (cx - state.transform.x);
                state.transform.y = cy - scaleDelta * (cy - state.transform.y);
                state.transform.scale = newScale;
                applyTransform(state);
            }
            return;
        }

        // Pan
        if (state.isPanning && state.pointers.size === 1) {
            const dx = e.clientX - state.panStart.x;
            const dy = e.clientY - state.panStart.y;
            state.transform.x = state.panTransformStart.x + dx;
            state.transform.y = state.panTransformStart.y + dy;
            applyTransform(state);
        }
    };

    state.handlers.pointerup = function (e) {
        state.pointers.delete(e.pointerId);
        clearLongPress(state);

        if (state.pointers.size < 2) {
            state.pinchStartDist = 0;
        }

        if (state.pointers.size === 0) {
            state.isPanning = false;
            svgEl.style.cursor = '';
        }
    };

    state.handlers.pointercancel = function (e) {
        state.pointers.delete(e.pointerId);
        clearLongPress(state);
        if (state.pointers.size === 0) {
            state.isPanning = false;
            svgEl.style.cursor = '';
        }
    };

    state.handlers.wheel = function (e) {
        e.preventDefault();
        const rect = svgEl.getBoundingClientRect();
        const cx = e.clientX - rect.left;
        const cy = e.clientY - rect.top;

        const zoomFactor = e.deltaY < 0 ? 1.1 : 1 / 1.1;
        let newScale = state.transform.scale * zoomFactor;
        newScale = Math.max(state.minZoom, Math.min(state.maxZoom, newScale));

        const scaleDelta = newScale / state.transform.scale;
        state.transform.x = cx - scaleDelta * (cx - state.transform.x);
        state.transform.y = cy - scaleDelta * (cy - state.transform.y);
        state.transform.scale = newScale;
        applyTransform(state);
    };

    // Attach event listeners
    svgEl.addEventListener('pointerdown', state.handlers.pointerdown);
    svgEl.addEventListener('pointermove', state.handlers.pointermove);
    svgEl.addEventListener('pointerup', state.handlers.pointerup);
    svgEl.addEventListener('pointercancel', state.handlers.pointercancel);
    svgEl.addEventListener('wheel', state.handlers.wheel, { passive: false });

    // Touch-action to prevent browser gestures interfering
    svgEl.style.touchAction = 'none';

    instances.set(svgId, state);
}

function clearLongPress(state) {
    if (state.longPressTimer) {
        clearTimeout(state.longPressTimer);
        state.longPressTimer = null;
    }
    state.longPressStartPos = null;
}

export function zoomToFit(svgId, contentWidth, contentHeight, padding) {
    const state = instances.get(svgId);
    if (!state) return;

    padding = padding || 40;
    const rect = state.svgEl.getBoundingClientRect();
    const viewWidth = rect.width;
    const viewHeight = rect.height;

    if (contentWidth <= 0 || contentHeight <= 0 || viewWidth <= 0 || viewHeight <= 0) {
        return;
    }

    const scaleX = (viewWidth - padding * 2) / contentWidth;
    const scaleY = (viewHeight - padding * 2) / contentHeight;
    const scale = Math.max(state.minZoom, Math.min(state.maxZoom, Math.min(scaleX, scaleY)));

    const tx = (viewWidth - contentWidth * scale) / 2;
    const ty = (viewHeight - contentHeight * scale) / 2;

    animateToTransform(state, tx, ty, scale, 300);
}

export function resetView(svgId, contentWidth, contentHeight) {
    zoomToFit(svgId, contentWidth, contentHeight, 40);
}

export function scrollToNode(svgId, nodeElementId) {
    const state = instances.get(svgId);
    if (!state) return false;

    const nodeEl = document.getElementById(nodeElementId);
    if (!nodeEl) return false;

    const rect = state.svgEl.getBoundingClientRect();
    const viewWidth = rect.width;
    const viewHeight = rect.height;

    // Get the node's transform to find its position
    const transformAttr = nodeEl.getAttribute('transform');
    if (!transformAttr) return false;

    const match = transformAttr.match(/translate\(([^,]+),([^)]+)\)/);
    if (!match) return false;

    const nodeX = parseFloat(match[1]);
    const nodeY = parseFloat(match[2]);

    // Center the node in the viewport
    const scale = state.transform.scale;
    const tx = viewWidth / 2 - nodeX * scale - 60 * scale; // offset for node width approximation
    const ty = viewHeight / 2 - nodeY * scale - 25 * scale; // offset for node height approximation

    animateToTransform(state, tx, ty, scale, 300);
    return true;
}

export function zoomToNode(svgId, nodeElementId, targetScale) {
    const state = instances.get(svgId);
    if (!state) return false;

    const nodeEl = document.getElementById(nodeElementId);
    if (!nodeEl) return false;

    const rect = state.svgEl.getBoundingClientRect();
    const viewWidth = rect.width;
    const viewHeight = rect.height;

    // Node position comes from its SVG transform attribute
    const transformAttr = nodeEl.getAttribute('transform');
    if (!transformAttr) return false;

    const match = transformAttr.match(/translate\(([^,]+),([^)]+)\)/);
    if (!match) return false;

    const nodeX = parseFloat(match[1]);
    const nodeY = parseFloat(match[2]);

    // Read actual node dimensions from the SVG bounding box
    let nodeW = 120, nodeH = 60;
    try {
        const bbox = nodeEl.getBBox();
        nodeW = bbox.width;
        nodeH = bbox.height;
    } catch (e) { /* getBBox not available (e.g. not yet rendered) */ }

    const scale = Math.max(state.minZoom, Math.min(state.maxZoom, targetScale || 1.5));
    const tx = viewWidth / 2 - (nodeX + nodeW / 2) * scale;
    const ty = viewHeight / 2 - (nodeY + nodeH / 2) * scale;

    animateToTransform(state, tx, ty, scale, 350);
    return true;
}

export function dispose(svgId) {
    const state = instances.get(svgId);
    if (!state) return;

    if (state.animationFrame) {
        cancelAnimationFrame(state.animationFrame);
    }
    clearLongPress(state);

    state.svgEl.removeEventListener('pointerdown', state.handlers.pointerdown);
    state.svgEl.removeEventListener('pointermove', state.handlers.pointermove);
    state.svgEl.removeEventListener('pointerup', state.handlers.pointerup);
    state.svgEl.removeEventListener('pointercancel', state.handlers.pointercancel);
    state.svgEl.removeEventListener('wheel', state.handlers.wheel);

    instances.delete(svgId);
}
