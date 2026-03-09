using System.Collections.Generic;
using System.Linq;
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal static class GraphStructureAnalyzer
    {
        public static GraphLayoutMode Detect(List<GraphNode> nodes, List<GraphEdge> edges)
        {
            if (nodes.Count == 0)
                return GraphLayoutMode.Forest;

            if (edges.Count == 0)
                return nodes.Count <= 1 ? GraphLayoutMode.HierarchicalTree : GraphLayoutMode.Forest;

            // All edges undirected → Network
            if (edges.All(e => e.Direction == EdgeDirection.Undirected))
                return GraphLayoutMode.Network;

            // Count incoming directed edges per node
            var incomingCount = new Dictionary<string, int>();
            foreach (var node in nodes)
                incomingCount[node.Id] = 0;

            foreach (var edge in edges)
            {
                if (edge.Direction == EdgeDirection.Directed || edge.Direction == EdgeDirection.Bidirectional)
                {
                    if (incomingCount.ContainsKey(edge.TargetNodeId))
                        incomingCount[edge.TargetNodeId]++;
                }
            }

            // Any node with >1 incoming → DAG
            if (incomingCount.Values.Any(c => c > 1))
                return GraphLayoutMode.DAG;

            // Count roots (0 incoming directed edges)
            int rootCount = incomingCount.Values.Count(c => c == 0);

            return rootCount <= 1 ? GraphLayoutMode.HierarchicalTree : GraphLayoutMode.Forest;
        }
    }
}
