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
            foreach (var kvp in depth)
            {
                var pn = positionedNodes[kvp.Key];
                pn.Y = kvp.Value * (pn.Height + options.VerticalSpacing);
            }

            // Position nodes bottom-up using post-order traversal
            var xCounter = 0.0;
            PositionSubtree(rootId, childrenMap, positionedNodes, depth, options, ref xCounter);

            // Handle nodes not in the tree (disconnected)
            foreach (var node in nodes)
            {
                if (node.Id != rootId && !incomingSet.Contains(node.Id) && !IsDescendant(node.Id, rootId, childrenMap))
                {
                    positionedNodes[node.Id].X = xCounter;
                    xCounter += positionedNodes[node.Id].Width + options.HorizontalSpacing;
                }
            }

            result.Nodes = positionedNodes.Values.ToList();

            // Generate edge paths
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
            ref double xCounter)
        {
            var children = childrenMap[nodeId].Where(c => depth.ContainsKey(c)).ToList();

            if (children.Count == 0)
            {
                // Leaf node: place at current x counter
                positionedNodes[nodeId].X = xCounter;
                xCounter += positionedNodes[nodeId].Width + options.HorizontalSpacing;
                return;
            }

            // Position all children first
            foreach (var childId in children)
            {
                PositionSubtree(childId, childrenMap, positionedNodes, depth, options, ref xCounter);
            }

            // Center parent over children
            var firstChild = positionedNodes[children.First()];
            var lastChild = positionedNodes[children.Last()];
            double childrenCenter = (firstChild.X + firstChild.Width / 2 + lastChild.X + lastChild.Width / 2) / 2;
            positionedNodes[nodeId].X = childrenCenter - positionedNodes[nodeId].Width / 2;
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
            bool isVertical = direction == LayoutDirection.TopToBottom || direction == LayoutDirection.BottomToTop;

            if (isVertical)
            {
                double midY = (startY + endY) / 2;
                double dx = endX - startX;

                if (Math.Abs(dx) < 0.1 || cornerRadius < 0.1)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "M {0},{1} L {0},{2} L {3},{2} L {3},{4}",
                        startX, startY, midY, endX, endY);
                }

                double r = Math.Min(cornerRadius, Math.Min(Math.Abs(midY - startY), Math.Abs(dx)) / 2);
                double sweepDown = dx > 0 ? 1 : 0;
                double sweepUp = dx > 0 ? 0 : 1;
                double signX = dx > 0 ? 1 : -1;

                return string.Format(CultureInfo.InvariantCulture,
                    "M {0},{1} L {0},{2} A {3},{3} 0 0 {4} {5},{6} L {7},{6} A {3},{3} 0 0 {8} {9},{10} L {9},{11}",
                    startX, startY,
                    midY - r,
                    r,
                    sweepDown,
                    startX + signX * r, midY,
                    endX - signX * r, midY,
                    sweepUp,
                    endX, midY + r,
                    endY);
            }
            else
            {
                double midX = (startX + endX) / 2;
                double dy = endY - startY;

                if (Math.Abs(dy) < 0.1 || cornerRadius < 0.1)
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "M {0},{1} L {2},{1} L {2},{3} L {4},{3}",
                        startX, startY, midX, endY, endX);
                }

                double r = Math.Min(cornerRadius, Math.Min(Math.Abs(midX - startX), Math.Abs(dy)) / 2);
                double sweepRight = dy > 0 ? 0 : 1;
                double sweepLeft = dy > 0 ? 1 : 0;
                double signY = dy > 0 ? 1 : -1;

                return string.Format(CultureInfo.InvariantCulture,
                    "M {0},{1} L {2},{1} A {3},{3} 0 0 {4} {5},{6} L {5},{7} A {3},{3} 0 0 {8} {9},{10} L {11},{10}",
                    startX, startY,
                    midX - r,
                    r,
                    sweepRight,
                    midX, startY + signY * r,
                    endY - signY * r,
                    sweepLeft,
                    midX + r, endY,
                    endX);
            }
        }
    }
}
