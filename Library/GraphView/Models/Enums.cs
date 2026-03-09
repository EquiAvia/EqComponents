namespace equiavia.components.Library.GraphView.Models
{
    public enum NodeStatus { None, Ok, Warning, Error, Unknown }
    public enum NodeShape { Auto, Circle, Rectangle, RoundedRectangle }
    public enum EdgeDirection { Undirected, Directed, Bidirectional }
    public enum GraphLayoutMode { Auto, HierarchicalTree, Forest, DAG, Network }

    public enum LayoutDirection
    {
        TopToBottom,
        BottomToTop,
        LeftToRight,
        RightToLeft
    }

    public enum EdgeRouting
    {
        Bezier,
        Straight,
        Orthogonal
    }
}
