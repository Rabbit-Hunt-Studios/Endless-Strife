using System;

namespace TbsFramework.Pathfinding.DataStructs
{
    /// <summary>
    /// Represents a prioritized queue.
    /// </summary>
    public interface IPriorityQueue<T>
    {
        /// <summary>
        /// Number of items in the queue.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Method adds item to the queue.
        /// </summary>
        void Enqueue(T item, float priority);
        /// <summary>
        /// Method returns item with the LOWEST priority value.
        /// </summary>
        T Dequeue();
    }

    /// <summary>
    /// Represents a node in a priority queue.
    /// </summary>
    readonly struct PriorityQueueNode<T> : IComparable<PriorityQueueNode<T>>
    {
        public readonly T Item;
        public readonly float Priority;

        public PriorityQueueNode(T item, float priority)
        {
            Item = item;
            Priority = priority;
        }

        public readonly int CompareTo(PriorityQueueNode<T> obj)
        {
            return Priority.CompareTo(obj.Priority);
        }
    }
}
