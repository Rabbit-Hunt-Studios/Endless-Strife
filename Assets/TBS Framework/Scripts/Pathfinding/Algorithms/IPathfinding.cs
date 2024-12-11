using System.Collections.Generic;
using System.Linq;
using TbsFramework.Pathfinding.DataStructs;

namespace TbsFramework.Pathfinding.Algorithms
{
    /// <summary>
    /// Provides the base structure for pathfinding algorithms.
    /// </summary>
    public abstract class IPathfinding
    {
        /// <summary>
        /// Finds the shortest path between the origin and destination nodes in the graph.
        /// </summary>
        /// <param name="edges">
        /// Represents the graph edges using nested dictionaries. The outer dictionary holds all nodes in the graph,
        /// while the inner dictionary maps neighbouring nodes to their respective edge weights.
        /// </param>
        /// <param name="originNode">The starting node of the pathfinding process.</param>
        /// <param name="destinationNode">The end node where the pathfinding process aims to reach.</param>
        /// <returns>
        /// A list of nodes representing the shortest path from origin to destination. If no path exists, an empty list is returned.
        /// </returns>
        public abstract IList<T> FindPath<T>(Dictionary<T, Dictionary<T, float>> edges, T originNode, T destinationNode) where T : IGraphNode;

        /// <summary>
        /// Retrieves the neighboring nodes of the given node from the graph.
        /// </summary>
        /// <param name="edges">The graph's edge structure, represented as nested dictionaries.</param>
        /// <param name="node">The node whose neighbors are to be retrieved.</param>
        /// <returns>An enumerable of neighboring nodes. If the node has no neighbors, an empty enumerable is returned.</returns>
        protected IEnumerable<T> GetNeigbours<T>(Dictionary<T, Dictionary<T, float>> edges, T node) where T : IGraphNode
        {
            if (edges.TryGetValue(node, out var neighbours))
            {
                return neighbours.Keys;
            }
            return Enumerable.Empty<T>();
        }
    }
}
