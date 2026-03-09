using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal class GraphLayoutOptions
    {
        public double DefaultNodeWidth { get; set; } = 120;
        public double DefaultNodeHeight { get; set; } = 60;
        public double CircleDiameter { get; set; } = 60;
        public double HorizontalSpacing { get; set; } = 40;
        public double VerticalSpacing { get; set; } = 60;
        public bool IsPerformanceMode { get; set; } = false;
        public LayoutDirection Direction { get; set; } = LayoutDirection.TopToBottom;
        public EdgeRouting EdgeRouting { get; set; } = EdgeRouting.Bezier;
        public double CornerRadius { get; set; } = 8;
    }
}
