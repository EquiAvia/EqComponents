using System.Collections.Generic;
using equiavia.components.Library.GraphView.Models;

namespace equiavia.components.Library.GraphView.Layout
{
    internal interface IGraphLayout
    {
        LayoutResult Calculate(List<GraphNode> nodes, List<GraphEdge> edges, GraphLayoutOptions options);
    }
}
