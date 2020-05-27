using System;
using System.Collections.Generic;

namespace Bonsai.Core
{
    public enum Traversal { PreOrder, PostOrder, LevelOrder };

    public class TreeIterator<T> where T : TreeIterator<T>.IterableNode
    {
        // For Pre and Post order.
        private Stack<T> _stackPath;

        // For level order.
        private Queue<T> _queuePath;

        // Used for PostOrder
        private HashSet<T> _visited;

        // The type of traversal the iterator is doing.
        private Traversal _traversal;

        // The current level that the tree is currently in (For level order traversal only).
        private int _currentLevel = 0;

        // Used in level traversal to know when we reached a new tree level.
        private int _queueNodeCount = 0;

        private Func<T, bool> _skipFilter;

        public TreeIterator(T root, Traversal type = Traversal.PreOrder)
        {
            if (root == null) {
                return;
            }

            _traversal = type;

            if (type == Traversal.LevelOrder) {
                _queuePath = new Queue<T>();
                _queuePath.Enqueue(root);
            }

            else {
                _stackPath = new Stack<T>();
                _stackPath.Push(root);

                if (type == Traversal.PostOrder) {
                    _visited = new HashSet<T>();
                }
            }
        }

        /// <summary>
        /// Iterates and returns the next node in the tree.
        /// </summary>
        /// <returns></returns>
        public T Next()
        {
            switch (_traversal) {

                case Traversal.PreOrder:
                    return preOrderNext();

                case Traversal.PostOrder:
                    return postOrderNext();

                default: return levelOrderNext();
            }
        }

        private T preOrderNext()
        {
            T current = _stackPath.Pop();

            for (int i = current.ChildCount() - 1; i >= 0; --i) {

                T child = current.GetChildAt(i);

                if (_skipFilter != null && _skipFilter(child)) {
                    continue;
                }

                _stackPath.Push(child);
            }

            return current;
        }

        private T postOrderNext()
        {
            T current = _stackPath.Peek();

            // Keep pushing until we reach a leaf.
            // Also do not re-traverse nodes that already had their children added.
            while (!_visited.Contains(current) && current.ChildCount() != 0) {

                for (int i = current.ChildCount() - 1; i >= 0; --i) {

                    T child = current.GetChildAt(i);

                    _stackPath.Push(child);
                }

                _visited.Add(current);
                current = _stackPath.Peek();
            }

            return _stackPath.Pop();
        }

        private T levelOrderNext()
        {
            // Keep dequeuing from the current level.
            if (_queueNodeCount > 0) {
                _queueNodeCount -= 1;
            }

            // Once we dequeued the entire level, we go down a level.
            if (_queueNodeCount == 0) {

                // Don't forget to adjust for skipping from filter in order
                // to keep the proper level.
                _queueNodeCount = _queuePath.Count;
                _currentLevel += 1;
            }

            T current = _queuePath.Dequeue();

            for (int i = 0; i < current.ChildCount(); ++i) {

                var child = current.GetChildAt(i);
                _queuePath.Enqueue(child);
            }

            return current;
        }

        /// <summary>
        /// Checks if there are still nodes to traverse.
        /// </summary>
        /// <returns></returns>
        public bool HasNext()
        {
            if (_stackPath != null) return _stackPath.Count != 0;
            if (_queuePath != null) return _queuePath.Count != 0;
            return false;
        }

        /// <summary>
        /// The current level of the tree that the iterator is in.
        /// It is offset by -1 so it matches the start of arrays.
        /// NOTE: Only works with level traversals. 
        /// </summary>
        public int CurrentLevel
        {
            get { return _currentLevel - 1; }
        }

        /// <summary>
        /// Interface for all nodes that can iterated.
        /// In order to not expose a list of children,
        /// classes implement two simple methods for the iterator to use.
        /// </summary>
        public interface IterableNode
        {
            /// <summary>
            /// Get the child at some index.
            /// This will allow the iterator to traverse in reverse
            /// the child list.
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            T GetChildAt(int index);

            /// <summary>
            /// Get the child count. This tells the iterator to iterate
            /// from the indices [0, ChildCount() ).
            /// </summary>
            /// <returns></returns>
            int ChildCount();
        }

        /// <summary>
        /// A helper method to traverse all nodes and execute an action per node.
        /// </summary>
        /// <param name="root">The travseral start.</param>
        /// <param name="onNext">The action to execute per node.</param>
        /// <param name="traversal">The type of DFS traversal.</param>
        public static void Traverse(T root, Action<T> onNext, Traversal traversal = Traversal.PreOrder)
        {
            var itr = new TreeIterator<T>(root, traversal);

            while (itr.HasNext()) {

                var node = itr.Next();
                onNext(node);
            }
        }

        /// <summary>
        /// A helper method to traverse all nodes and execute an action per node.
        /// This method also passes the iterator doing the traversal.
        /// </summary>
        /// <param name="root">The traversal start.</param>
        /// <param name="onNext">The action to execute per node.</param>
        /// <param name="traversal">The type of DFS traversal.</param>
        public static void Traverse(T root, Action<T, TreeIterator<T>> onNext, Traversal traversal = Traversal.PreOrder)
        {
            var itr = new TreeIterator<T>(root, traversal);

            while (itr.HasNext()) {

                var node = itr.Next();
                onNext(node, itr);
            }
        }

        /// <summary>
        /// A pre-order traversal with the option to skip some nodes.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="onNext"></param>
        /// <param name="skipFilter"></param>
        public static void Traverse(T root, Action<T> onNext, Func<T, bool> skipFilter)
        {
            if (skipFilter(root)) {
                return;
            }

            var itr = new TreeIterator<T>(root);

            itr._skipFilter = skipFilter;

            while (itr.HasNext()) {

                var node = itr.Next();
                onNext(node);
            }
        }
    }
}
