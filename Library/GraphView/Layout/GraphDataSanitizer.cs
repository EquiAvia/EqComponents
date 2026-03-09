using System;
using System.Collections.Generic;
using System.Linq;
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal static class GraphDataSanitizer
    {
        public static GraphData Sanitize(GraphData data, Action<string> onWarning)
        {
            if (data == null)
                return new GraphData();

            var result = new GraphData();

            var seenIds = new HashSet<string>();
            foreach (var node in data.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    onWarning($"Node with label '{node.Label}' has no ID and was discarded.");
                    continue;
                }
                if (!seenIds.Add(node.Id))
                {
                    onWarning($"Duplicate node ID '{node.Id}' discarded (first instance kept).");
                    continue;
                }
                result.Nodes.Add(node);
            }

            var validNodeIds = seenIds;
            var validEdges = new List<GraphEdge>();
            foreach (var edge in data.Edges)
            {
                if (string.IsNullOrWhiteSpace(edge.SourceNodeId) || string.IsNullOrWhiteSpace(edge.TargetNodeId))
                {
                    onWarning($"Edge '{edge.Id}' has empty source or target and was discarded.");
                    continue;
                }
                if (edge.SourceNodeId == edge.TargetNodeId)
                {
                    onWarning($"Edge '{edge.Id}' is self-referencing and was discarded.");
                    continue;
                }
                if (!validNodeIds.Contains(edge.SourceNodeId) || !validNodeIds.Contains(edge.TargetNodeId))
                {
                    onWarning($"Edge '{edge.Id}' references missing node(s) and was discarded.");
                    continue;
                }
                validEdges.Add(edge);
            }

            result.Edges = RemoveCyclicEdges(result.Nodes, validEdges, onWarning);
            return result;
        }

        private static List<GraphEdge> RemoveCyclicEdges(
            List<GraphNode> nodes, List<GraphEdge> edges, Action<string> onWarning)
        {
            var adjacency = new Dictionary<string, List<(string Target, GraphEdge Edge)>>();
            foreach (var node in nodes)
                adjacency[node.Id] = new List<(string, GraphEdge)>();
            foreach (var edge in edges)
            {
                if (adjacency.ContainsKey(edge.SourceNodeId))
                    adjacency[edge.SourceNodeId].Add((edge.TargetNodeId, edge));
            }

            var result = new List<GraphEdge>(edges);
            var visited = new HashSet<string>();
            var inStack = new HashSet<string>();

            foreach (var node in nodes)
            {
                if (!visited.Contains(node.Id))
                    DfsFindBackEdges(node.Id, adjacency, visited, inStack, result, onWarning);
            }

            return result;
        }

        private static void DfsFindBackEdges(
            string nodeId,
            Dictionary<string, List<(string Target, GraphEdge Edge)>> adjacency,
            HashSet<string> visited,
            HashSet<string> inStack,
            List<GraphEdge> edges,
            Action<string> onWarning)
        {
            visited.Add(nodeId);
            inStack.Add(nodeId);

            if (adjacency.TryGetValue(nodeId, out var neighbors))
            {
                foreach (var (target, edge) in neighbors.ToList())
                {
                    if (inStack.Contains(target))
                    {
                        edges.Remove(edge);
                        onWarning($"Edge '{edge.Id}' ({edge.SourceNodeId}\u2192{edge.TargetNodeId}) creates a cycle and was removed.");
                    }
                    else if (!visited.Contains(target))
                    {
                        DfsFindBackEdges(target, adjacency, visited, inStack, edges, onWarning);
                    }
                }
            }

            inStack.Remove(nodeId);
        }
    }
}
