using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal class HierarchicalTreeLayout : IGraphLayout
    {
        public LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options)
        {
            var result = new LayoutResult();

            if (nodes.Count == 0)
                return result;

            bool isHorizontal = options.Direction == LayoutDirection.LeftToRight
                             || options.Direction == LayoutDirection.RightToLeft;

            // Build parent→children map from directed edges
            var childrenMap = new Dictionary<string, List<string>>();
            var incomingSet = new HashSet<string>();

            foreach (var node in nodes)
                childrenMap[node.Id] = new List<string>();

            foreach (var edge in edges)
            {
                if (childrenMap.ContainsKey(edge.SourceNodeId))
                    childrenMap[edge.SourceNodeId].Add(edge.TargetNodeId);
                incomingSet.Add(edge.TargetNodeId);
            }

            // Find root: first node with no incoming edges
            var rootId = nodes.FirstOrDefault(n => !incomingSet.Contains(n.Id))?.Id ?? nodes[0].Id;

            // Build positioned nodes with dimensions
            var positionedNodes = new Dictionary<string, PositionedNode>();
            foreach (var node in nodes)
            {
                var shape = ResolveShape(node.Shape);
                var pn = new PositionedNode
                {
                    Node = node,
                    Width = shape == NodeShape.Circle ? options.CircleDiameter : options.DefaultNodeWidth,
                    Height = shape == NodeShape.Circle ? options.CircleDiameter : options.DefaultNodeHeight
                };
                positionedNodes[node.Id] = pn;
            }

            // Assign layers (depth) via BFS from root
            var depth = new Dictionary<string, int>();
            var queue = new Queue<string>();
            queue.Enqueue(rootId);
            depth[rootId] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var childId in childrenMap[current])
                {
                    if (!depth.ContainsKey(childId))
                    {
                        depth[childId] = depth[current] + 1;
                        queue.Enqueue(childId);
                    }
                }
            }

            // Handle disconnected nodes - assign them depth 0
            foreach (var node in nodes)
            {
                if (!depth.ContainsKey(node.Id))
                    depth[node.Id] = 0;
            }

            // Set Y positions based on depth
            // For LR/RL, depth runs along the X axis after transform,
            // so use Width + HorizontalSpacing for proper spacing
            foreach (var kvp in depth)
            {
                var pn = positionedNodes[kvp.Key];
                double depthSpacing = isHorizontal
                    ? pn.Width + options.HorizontalSpacing
                    : pn.Height + options.VerticalSpacing;
                pn.Y = kvp.Value * depthSpacing;
            }

            // Position nodes bottom-up using post-order traversal
            // For LR/RL, siblings stack along Y after transform,
            // so use Height + VerticalSpacing for sibling spacing
            var xCounter = 0.0;
            PositionSubtree(rootId, childrenMap, positionedNodes, depth, options, isHorizontal, ref xCounter);

            // Handle nodes not in the tree (disconnected)
            foreach (var node in nodes)
            {
                if (node.Id != rootId && !incomingSet.Contains(node.Id) && !IsDescendant(node.Id, rootId, childrenMap))
                {
                    positionedNodes[node.Id].X = xCounter;
                    double siblingDim = isHorizontal
                        ? positionedNodes[node.Id].Height + options.VerticalSpacing
                        : positionedNodes[node.Id].Width + options.HorizontalSpacing;
                    xCounter += siblingDim;
                }
            }

            result.Nodes = positionedNodes.Values.ToList();

            // Apply direction transform BEFORE edge generation
            if (options.Direction != LayoutDirection.TopToBottom)
            {
                TransformForDirection(result, options.Direction);
                positionedNodes = result.Nodes.ToDictionary(n => n.Node.Id);
            }

            // Generate edge paths (uses transformed positions + direction-aware connection points)
            foreach (var edge in edges)
            {
                if (positionedNodes.ContainsKey(edge.SourceNodeId) && positionedNodes.ContainsKey(edge.TargetNodeId))
                {
                    var source = positionedNodes[edge.SourceNodeId];
                    var target = positionedNodes[edge.TargetNodeId];
                    var edgePath = BuildEdgePath(source, target, edge, options);
                    result.Edges.Add(edgePath);
                }
            }

            // Compute total dimensions
            if (result.Nodes.Count > 0)
            {
                result.TotalWidth = result.Nodes.Max(n => n.X + n.Width);
                result.TotalHeight = result.Nodes.Max(n => n.Y + n.Height);
            }

            return result;
        }

        private static void PositionSubtree(
            string nodeId,
            Dictionary<string, List<string>> childrenMap,
            Dictionary<string, PositionedNode> positionedNodes,
            Dictionary<string, int> depth,
            GraphLayoutOptions options,
            bool isHorizontal,
            ref double xCounter)
        {
            var children = childrenMap[nodeId].Where(c => depth.ContainsKey(c)).ToList();

            if (children.Count == 0)
            {
                // Leaf node: place at current x counter
                positionedNodes[nodeId].X = xCounter;
                double siblingDim = isHorizontal
                    ? positionedNodes[nodeId].Height + options.VerticalSpacing
                    : positionedNodes[nodeId].Width + options.HorizontalSpacing;
                xCounter += siblingDim;
                return;
            }

            // Position all children first
            foreach (var childId in children)
            {
                PositionSubtree(childId, childrenMap, positionedNodes, depth, options, isHorizontal, ref xCounter);
            }

            // Center parent over children
            var firstChild = positionedNodes[children.First()];
            var lastChild = positionedNodes[children.Last()];
            double halfDim = isHorizontal
                ? positionedNodes[nodeId].Height / 2
                : positionedNodes[nodeId].Width / 2;
            double halfFirst = isHorizontal ? firstChild.Height / 2 : firstChild.Width / 2;
            double halfLast = isHorizontal ? lastChild.Height / 2 : lastChild.Width / 2;
            double childrenCenter = (firstChild.X + halfFirst + lastChild.X + halfLast) / 2;
            positionedNodes[nodeId].X = childrenCenter - halfDim;
        }

        private static bool IsDescendant(string nodeId, string rootId, Dictionary<string, List<string>> childrenMap)
        {
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == nodeId) return true;
                if (!visited.Add(current)) continue;
                foreach (var child in childrenMap[current])
                    stack.Push(child);
            }

            return false;
        }

        private static NodeShape ResolveShape(NodeShape shape)
        {
            return shape == NodeShape.Auto ? NodeShape.RoundedRectangle : shape;
        }

        private static void TransformForDirection(LayoutResult result, LayoutDirection direction)
        {
            double maxY = result.Nodes.Max(n => n.Y + n.Height);

            switch (direction)
            {
                case LayoutDirection.BottomToTop:
                    foreach (var node in result.Nodes)
                        node.Y = maxY - node.Y - node.Height;
                    break;

                case LayoutDirection.LeftToRight:
                    // Swap X/Y coordinates only — dimensions stay the same
                    // because Calculate already used direction-aware spacing
                    foreach (var node in result.Nodes)
                    {
                        (node.X, node.Y) = (node.Y, node.X);
                    }
                    break;

                case LayoutDirection.RightToLeft:
                    // Swap X/Y then mirror along X axis
                    double rtlMaxX = result.Nodes.Max(n => n.Y + n.Width);
                    foreach (var node in result.Nodes)
                    {
                        double newX = node.Y;
                        double newY = node.X;
                        node.X = rtlMaxX - newX - node.Width;
                        node.Y = newY;
                    }
                    break;
            }
        }

        internal static EdgePath BuildEdgePath(PositionedNode source, PositionedNode target, GraphEdge edge, GraphLayoutOptions options)
        {
            double startX, startY, endX, endY;

            switch (options.Direction)
            {
                case LayoutDirection.BottomToTop:
                    startX = source.X + source.Width / 2;
                    startY = source.Y;
                    endX = target.X + target.Width / 2;
                    endY = target.Y + target.Height;
                    break;
                case LayoutDirection.LeftToRight:
                    startX = source.X + source.Width;
                    startY = source.Y + source.Height / 2;
                    endX = target.X;
                    endY = target.Y + target.Height / 2;
                    break;
                case LayoutDirection.RightToLeft:
                    startX = source.X;
                    startY = source.Y + source.Height / 2;
                    endX = target.X + target.Width;
                    endY = target.Y + target.Height / 2;
                    break;
                default: // TopToBottom
                    startX = source.X + source.Width / 2;
                    startY = source.Y + source.Height;
                    endX = target.X + target.Width / 2;
                    endY = target.Y;
                    break;
            }

            string svgPath = options.EdgeRouting switch
            {
                EdgeRouting.Straight => BuildStraightPath(startX, startY, endX, endY),
                EdgeRouting.Orthogonal => BuildOrthogonalPath(startX, startY, endX, endY, options.Direction, options.CornerRadius),
                _ => BuildBezierPath(startX, startY, endX, endY, options.Direction)
            };

            return new EdgePath
            {
                Edge = edge,
                SvgPath = svgPath,
                LabelX = (startX + endX) / 2,
                LabelY = (startY + endY) / 2
            };
        }

        private static string BuildBezierPath(double startX, double startY, double endX, double endY, LayoutDirection direction)
        {
            bool isVertical = direction == LayoutDirection.TopToBottom || direction == LayoutDirection.BottomToTop;

            if (isVertical)
            {
                double midY = (startY + endY) / 2;
                return string.Format(CultureInfo.InvariantCulture,
                    "M {0},{1} C {0},{2} {3},{2} {3},{4}",
                    startX, startY, midY, endX, endY);
            }
            else
            {
                double midX = (startX + endX) / 2;
                return string.Format(CultureInfo.InvariantCulture,
                    "M {0},{1} C {2},{1} {2},{3} {4},{3}",
                    startX, startY, midX, endY, endX);
            }
        }

        private static string BuildStraightPath(double startX, double startY, double endX, double endY)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "M {0},{1} L {2},{3}",
                startX, startY, endX, endY);
        }

        private static string BuildOrthogonalPath(double startX, double startY, double endX, double endY, LayoutDirection direction, double cornerRadius)
        {
            // L-shaped routing: short stem from parent, then turn toward child.
            // This avoids the "bus junction" artifact of Z-shaped (midpoint) routing
            // where multiple siblings share the same horizontal/vertical segment.
            bool isVertical = direction == LayoutDirection.TopToBottom || direction == LayoutDirection.BottomToTop;

            if (isVertical)
            {
                // Vertical flow: drop down from parent to endY, then horizontal to child X
                double dx = endX - startX;

                // Straight line if aligned
                if (Math.Abs(dx) < 0.1)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "M {0},{1} L {0},{2}",
                        startX, startY, endY);
                }

                // L-shape: vertical from start down to endY level, then horizontal to endX
                if (cornerRadius < 0.1)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "M {0},{1} L {0},{2} L {3},{2}",
                        startX, startY, endY, endX);
                }

                double verticalDist = Math.Abs(endY - startY);
                double r = Math.Min(cornerRadius, Math.Min(verticalDist, Math.Abs(dx)) / 2);
                double signX = dx > 0 ? 1 : -1;
                double signY = endY > startY ? 1 : -1;
                // Sweep flag for the single corner
                double sweep = (signY > 0 && signX > 0) || (signY < 0 && signX < 0) ? 1 : 0;

                return string.Format(CultureInfo.InvariantCulture,
                    "M {0},{1} L {0},{2} A {3},{3} 0 0 {4} {5},{6} L {7},{6}",
                    startX, startY,
                    endY - signY * r,
                    r,
                    sweep,
                    startX + signX * r, endY,
                    endX);
            }
            else
            {
                // Horizontal flow: go right from parent to endX, then vertical to child Y
                double dy = endY - startY;

                // Straight line if aligned
                if (Math.Abs(dy) < 0.1)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "M {0},{1} L {2},{1}",
                        startX, startY, endX);
                }

                // L-shape: horizontal from start to endX level, then vertical to endY
                if (cornerRadius < 0.1)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "M {0},{1} L {2},{1} L {2},{3}",
                        startX, startY, endX, endY);
                }

                double horizontalDist = Math.Abs(endX - startX);
                double r = Math.Min(cornerRadius, Math.Min(horizontalDist, Math.Abs(dy)) / 2);
                double signX = endX > startX ? 1 : -1;
                double signY = dy > 0 ? 1 : -1;
                double sweep = (signX > 0 && signY > 0) || (signX < 0 && signY < 0) ? 0 : 1;

                return string.Format(CultureInfo.InvariantCulture,
                    "M {0},{1} L {2},{1} A {3},{3} 0 0 {4} {5},{6} L {5},{7}",
                    startX, startY,
                    endX - signX * r,
                    r,
                    sweep,
                    endX, startY + signY * r,
                    endY);
            }
        }
    }
}
