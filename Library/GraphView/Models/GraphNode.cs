using System.Collections.Generic;

namespace equiavia.components.Library.GraphView.Models
{
    public class GraphNode
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string LogoUrl { get; set; }
        public NodeStatus Status { get; set; } = NodeStatus.None;
        public NodeShape Shape { get; set; } = NodeShape.Auto;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
