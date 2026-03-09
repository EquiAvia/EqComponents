using System.Collections.Generic;

namespace equiavia.components.Library.GraphView.Models
{
    public class GraphEdge
    {
        public string Id { get; set; } = string.Empty;
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string Label { get; set; }
        public EdgeDirection Direction { get; set; } = EdgeDirection.Directed;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
