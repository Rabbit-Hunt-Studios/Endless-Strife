using System.Collections.Generic;

namespace TbsFramework.Pathfinding.DataStructs
{
    /// <summary>
    /// Implementation of priority queue based on binary heap.
    /// </summary>
    class HeapPriorityQueue<T> : IPriorityQueue<T>
    {
        private readonly List<(float Priority, T Item)> _queue;

        public HeapPriorityQueue(int initialCapacity = 0)
        {
            _queue = new List<(float Priority, T Item)>(initialCapacity);
        }

        public int Count
        {
            get { return _queue.Count; }
        }

        public void Enqueue(T item, float priority)
        {
            _queue.Add((priority, item));
            int childIndex = _queue.Count - 1;
            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;
                if (_queue[childIndex].Priority >= _queue[parentIndex].Priority)
                    break;
                (_queue[parentIndex], _queue[childIndex]) = (_queue[childIndex], _queue[parentIndex]);
                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = _queue.Count - 1;
            var frontItem = _queue[0].Item;
            _queue[0] = _queue[lastIndex];
            _queue.RemoveAt(lastIndex);

            --lastIndex;
            int parentIndex = 0;
            while (true)
            {
                int leftChildIndex = parentIndex * 2 + 1;
                if (leftChildIndex > lastIndex) break;
                int rightChildIndex = leftChildIndex + 1;
                if (rightChildIndex <= lastIndex && _queue[rightChildIndex].Priority < _queue[leftChildIndex].Priority)
                    leftChildIndex = rightChildIndex;
                if (_queue[parentIndex].Priority <= _queue[leftChildIndex].Priority) break;
                (_queue[leftChildIndex], _queue[parentIndex]) = (_queue[parentIndex], _queue[leftChildIndex]);
                parentIndex = leftChildIndex;
            }
            return frontItem;
        }
    }
}
