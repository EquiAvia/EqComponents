using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal class ForestLayout : IGraphLayout
    {
        public LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options)
        {
            var result = new LayoutResult();

            if (nodes.Count == 0)
                return result;

            // Find weakly connected components via BFS (treat all edges as undirected)
            var components = FindComponents(nodes, edges);

            var hierarchicalLayout = new HierarchicalTreeLayout();
            double xOffset = 0;
            double yOffset = 0;

            bool isVerticalFlow = options.Direction == LayoutDirection.TopToBottom
                               || options.Direction == LayoutDirection.BottomToTop;

            foreach (var component in components)
            {
                var componentNodeIds = new HashSet<string>(component.Select(n => n.Id));
                var componentEdges = edges
                    .Where(e => componentNodeIds.Contains(e.SourceNodeId) && componentNodeIds.Contains(e.TargetNodeId))
                    .ToList();

                var componentResult = hierarchicalLayout.Calculate(component, componentEdges, options);

                if (isVerticalFlow)
                {
                    // Components arranged left-to-right (offset X)
                    foreach (var pn in componentResult.Nodes)
                    {
                        pn.X += xOffset;
                        result.Nodes.Add(pn);
                    }

                    foreach (var ep in componentResult.Edges)
                    {
                        if (xOffset > 0)
                            ep.SvgPath = OffsetSvgPath(ep.SvgPath, xOffset, 0);
                        ep.LabelX += xOffset;
                        result.Edges.Add(ep);
                    }

                    double componentWidth = componentResult.TotalWidth;
                    if (componentWidth <= 0 && componentResult.Nodes.Count > 0)
                        componentWidth = componentResult.Nodes.Max(n => n.X + n.Width) - componentResult.Nodes.Min(n => n.X);
                    xOffset += componentWidth + options.HorizontalSpacing;
                }
                else
                {
                    // Components arranged top-to-bottom (offset Y) for LR/RL
                    foreach (var pn in componentResult.Nodes)
                    {
                        pn.Y += yOffset;
                        result.Nodes.Add(pn);
                    }

                    foreach (var ep in componentResult.Edges)
                    {
                        if (yOffset > 0)
                            ep.SvgPath = OffsetSvgPath(ep.SvgPath, 0, yOffset);
                        ep.LabelY += yOffset;
                        result.Edges.Add(ep);
                    }

                    double componentHeight = componentResult.TotalHeight;
                    if (componentHeight <= 0 && componentResult.Nodes.Count > 0)
                        componentHeight = componentResult.Nodes.Max(n => n.Y + n.Height) - componentResult.Nodes.Min(n => n.Y);
                    yOffset += componentHeight + options.VerticalSpacing;
                }
            }

            // Calculate total dimensions
            if (result.Nodes.Count > 0)
            {
                result.TotalWidth = result.Nodes.Max(n => n.X + n.Width);
                result.TotalHeight = result.Nodes.Max(n => n.Y + n.Height);
            }

            return result;
        }

        private static List<List<GraphNode>> FindComponents(List<GraphNode> nodes, List<GraphEdge> edges)
        {
            // Build adjacency list (undirected)
            var adjacency = new Dictionary<string, List<string>>();
            foreach (var node in nodes)
                adjacency[node.Id] = new List<string>();

            foreach (var edge in edges)
            {
                if (adjacency.ContainsKey(edge.SourceNodeId) && adjacency.ContainsKey(edge.TargetNodeId))
                {
                    adjacency[edge.SourceNodeId].Add(edge.TargetNodeId);
                    adjacency[edge.TargetNodeId].Add(edge.SourceNodeId);
                }
            }

            var visited = new HashSet<string>();
            var components = new List<List<GraphNode>>();
            var nodeMap = nodes.ToDictionary(n => n.Id);

            // Process in data order to maintain appearance order
            foreach (var node in nodes)
            {
                if (visited.Contains(node.Id))
                    continue;

                var component = new List<GraphNode>();
                var queue = new Queue<string>();
                queue.Enqueue(node.Id);
                visited.Add(node.Id);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(nodeMap[current]);

                    foreach (var neighbor in adjacency[current])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }

        private static string OffsetSvgPath(string path, double offsetX, double offsetY)
        {
            if (offsetX == 0 && offsetY == 0)
                return path;

            var parts = path.Split(' ');
            var result = new List<string>();

            foreach (var part in parts)
            {
                if (part.Contains(','))
                {
                    var coords = part.Split(',');
                    if (coords.Length == 2 &&
                        double.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                        double.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                    {
                        result.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1}", x + offsetX, y + offsetY));
                    }
                    else
                    {
                        result.Add(part);
                    }
                }
                else
                {
                    result.Add(part);
                }
            }

            return string.Join(" ", result);
        }
    }
}
