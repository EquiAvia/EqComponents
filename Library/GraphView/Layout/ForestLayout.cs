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
            double maxHeight = 0;

            foreach (var component in components)
            {
                var componentNodeIds = new HashSet<string>(component.Select(n => n.Id));
                var componentEdges = edges
                    .Where(e => componentNodeIds.Contains(e.SourceNodeId) && componentNodeIds.Contains(e.TargetNodeId))
                    .ToList();

                var componentResult = hierarchicalLayout.Calculate(component, componentEdges, options);

                // Offset node X positions
                foreach (var pn in componentResult.Nodes)
                {
                    pn.X += xOffset;
                    result.Nodes.Add(pn);
                }

                // Offset edge SVG path X coordinates
                foreach (var ep in componentResult.Edges)
                {
                    if (xOffset > 0)
                        ep.SvgPath = OffsetSvgPathX(ep.SvgPath, xOffset);

                    ep.LabelX += xOffset;
                    result.Edges.Add(ep);
                }

                double componentWidth = componentResult.TotalWidth;
                if (componentWidth <= 0 && componentResult.Nodes.Count > 0)
                    componentWidth = componentResult.Nodes.Max(n => n.X + n.Width) - componentResult.Nodes.Min(n => n.X);

                xOffset += componentWidth + options.HorizontalSpacing;

                if (componentResult.TotalHeight > maxHeight)
                    maxHeight = componentResult.TotalHeight;
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

        private static string OffsetSvgPathX(string path, double offset)
        {
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
                        result.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1}", x + offset, y));
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
