using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Pathfinding.DataStructs;

namespace TbsFramework.Pathfinding.Algorithms
{
    /// <summary>
    /// Implementation of Dijkstra pathfinding algorithm.
    /// </summary>
    class DijkstraPathfinding : IPathfinding
    {
        public Dictionary<Cell, IList<Cell>> FindAllPaths(Dictionary<Cell, Dictionary<Cell, float>> edges, Cell originNode)
        {
            IPriorityQueue<Cell> frontier = new HeapPriorityQueue<Cell>(edges.Count);
            frontier.Enqueue(originNode, 0);

            Dictionary<Cell, Cell> cameFrom = new Dictionary<Cell, Cell>(edges.Count);
            cameFrom.Add(originNode, default(Cell));
            Dictionary<Cell, float> costSoFar = new Dictionary<Cell, float>(edges.Count);
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeigbours(edges, current);
                var currentCost = costSoFar[current];
                var currentEdges = edges[current];

                foreach (var neighbour in neighbours)
                {
                    var newCost = currentCost + currentEdges[neighbour];
                    if (!costSoFar.TryGetValue(neighbour, out var neighbourCost) || newCost < neighbourCost)
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
            }

            Dictionary<Cell, IList<Cell>> paths = new Dictionary<Cell, IList<Cell>>();
            foreach (Cell destination in cameFrom.Keys)
            {
                List<Cell> path = new List<Cell>();
                var current = destination;
                while (current != null)
                {
                    path.Add(current);
                    current = cameFrom[current];
                }
                path.RemoveAt(path.Count - 1);
                paths.Add(destination, path);
            }
            return paths;
        }
        public override IList<T> FindPath<T>(Dictionary<T, Dictionary<T, float>> edges, T originNode, T destinationNode)
        {
            IPriorityQueue<T> frontier = new SortedListPriorityQueue<T>(edges.Count);
            frontier.Enqueue(originNode, 0);

            Dictionary<T, T> cameFrom = new Dictionary<T, T>(edges.Count);
            cameFrom.Add(originNode, default(T));
            Dictionary<T, float> costSoFar = new Dictionary<T, float>(edges.Count);
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeigbours(edges, current);
                var currentCost = costSoFar[current];
                var currentEdges = edges[current];

                foreach (var neighbour in neighbours)
                {
                    var newCost = currentCost + currentEdges[neighbour];
                    if (!costSoFar.TryGetValue(neighbour, out var neighbourCost) || newCost < neighbourCost)
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
                if (current.Equals(destinationNode)) break;
            }
            List<T> path = new List<T>();
            if (!cameFrom.ContainsKey(destinationNode))
                return path;

            path.Add(destinationNode);
            var temp = destinationNode;

            while (!cameFrom[temp].Equals(originNode))
            {
                var currentPathElement = cameFrom[temp];
                path.Add(currentPathElement);

                temp = currentPathElement;
            }

            return path;
        }
    }
}