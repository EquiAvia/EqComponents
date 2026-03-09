using System.Collections.Generic;

namespace equiavia.components.Library.GraphView.Models
{
    public class GraphData
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }
}
