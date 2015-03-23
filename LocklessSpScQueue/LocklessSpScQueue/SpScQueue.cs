using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace LocklessSpScQueue {

    /// <summary>
    /// A first in first out queue that is thread safe for a single concurrent writer and single reader.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [StructLayout(LayoutKind.Sequential)]   // Ensures cache padding
    public class SpScQueue<T> {

        [StructLayout(LayoutKind.Sequential)]   // Ensures cache padding
        private class Node<T> {
            public Node<T> Next;
            public T Item;

            public Node(T item) {
                Item = item;
            }
        }

#pragma warning disable 0169, 0414

        // The first free node
        private long d0, d1, d2, d3, d4, d5, d6, d7;
        private Node<T> _first;
        private long d8, d9, d10, d11, d12, d13, d14, d15;

        // Front of the list
        private long a0, a1, a2, a3, a4, a5, a6, a7;
        private Node<T> _head;
        private long a8, a9, a10, a11, a12, a13, a14, a15;

        // the actual back of the free list
        private long b0, b1, b2, b3, b4, b5, b6, b7;
        private Node<T> _tailCopy;
        private long b8, b9, b10, b11, b12, b13, b14, b15;

        // Back of the list
        private long c0, c1, c2, c3, c4, c5, c6, c7;
        private Node<T> _tail;
        private long c8, c9, c10, c11, c12, c13, c14, c15;

#pragma warning restore 0169, 0414

        /// <summary>
        /// Initialises a new instance of the <seealso cref="SpScQueue"/> class.
        /// </summary>
        public SpScQueue() {
            _head = new Node<T>(default(T));
            _first = _head;
            _tail = _head;
            _tailCopy = _tail;
        }

        private Node<T> AllocNode(T item) {
            // Try re-use a node from the cached set of free items
            if (_first != _tailCopy) {
                var n = _first;
                _first = _first.Next;

                n.Item = item;
                n.Next = null;

                return n;
            }

            // Update the pointer to the front of the tail.
            var v = _tail;

            // Ensure compiler & CPU write ordering at this point. All memory access above
            // will have completed before we assign the new node to the list
            Thread.MemoryBarrier();
            _tailCopy = v;

            // Try again
            if (_first != _tailCopy) {
                var n = _first;
                _first = _first.Next;

                n.Item = item;
                n.Next = null;

                return n;
            }

            // Return a new node
            return new Node<T>(item);
        }

        /// <summary>
        /// Adds the specified item to the queue.
        /// </summary>
        /// <param name="item">The item to add to the queue.</param>
        public void Enqueue(T item) {
            var node = AllocNode(item);

            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            } finally {
                // Ensure compiler & CPU write ordering at this point. All memory access above
                // will have completed before we assign the new node to the list
                Thread.MemoryBarrier();
                _head.Next = node;

                _head = node;
            }
        }

        /// <summary>
        /// Removes an item from the queue.
        /// </summary>
        /// <returns></returns>
        public T Dequeue() {
            T item;
            TryDequeue(out item);
            return item;
        }

        /// <summary>
        /// Tries to remove and return an item from the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if an item was returned, false if the queue was empty.</returns>
        public bool TryDequeue(out T item) {
            var next = _tail.Next;

            // Ensure compiler & CPU read ordering at this point. The read above
            // will have completed before the execution of the lines below.
            Thread.MemoryBarrier();

            if (next == null) {
                item = default(T);
                return false;
            }

            item = next.Item;

            // Ensure compiler & CPU write ordering at this point. The write above will
            // have been completed before the execution of the lines below. This
            // ensures that the item has been read into the temporary value before
            // the node is placed back on to the re-use queue when the _tail pointer
            // is advanced. If the read happens after the write we could corrupt the data
            // held in the node.
            Thread.MemoryBarrier();
            _tail = next;

            return true;
        }
    }
}
