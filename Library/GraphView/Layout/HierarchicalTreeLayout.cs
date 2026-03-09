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

                    double startX = source.X + source.Width / 2;
                    double startY = source.Y + source.Height;
                    double endX = target.X + target.Width / 2;
                    double endY = target.Y;
                    double midY = (startY + endY) / 2;

                    var svgPath = string.Format(
                        CultureInfo.InvariantCulture,
                        "M {0},{1} C {0},{2} {3},{2} {3},{4}",
                        startX, startY, midY, endX, endY);

                    result.Edges.Add(new EdgePath
                    {
                        Edge = edge,
                        SvgPath = svgPath,
                        LabelX = (startX + endX) / 2,
                        LabelY = (startY + endY) / 2
                    });
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
    }
}
