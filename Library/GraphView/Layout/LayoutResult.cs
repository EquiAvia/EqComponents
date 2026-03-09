using System.Collections.Generic;

namespace equiavia.components.Library.GraphView.Layout
{
    internal class PositionedNode
    {
        public Models.GraphNode Node { get; set; } = default!;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    internal class EdgePath
    {
        public Models.GraphEdge Edge { get; set; } = default!;
        public string SvgPath { get; set; } = string.Empty;
        public double LabelX { get; set; }
        public double LabelY { get; set; }
    }

    internal class LayoutResult
    {
        public List<PositionedNode> Nodes { get; set; } = new();
        public List<EdgePath> Edges { get; set; } = new();
        public double TotalWidth { get; set; }
        public double TotalHeight { get; set; }
    }
}
