using System.Collections.Generic;

namespace equiavia.components.Library.GraphView.Models
{
    public class GraphContextAction
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string IconUrl { get; set; }
        public bool IsSeparator { get; set; } = false;
        public List<GraphContextAction> Children { get; set; } = new();
    }
}
