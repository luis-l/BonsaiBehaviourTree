using System;
using System.Collections.Generic;

namespace Bonsai.Core
{
    /// <summary>
    /// A special iterator to handle traversing a behaviour tree.
    /// </summary>
    public sealed class BehaviourIterator
    {
        // Keeps track of the traversal path.
        // Useful to help on aborts and interrupts.
        private IntStack _traversal;
        
        // Access to the tree so we can find any node from pre-order index.
        private BehaviourTree _tree;

        private BehaviourNode.Status _lastStatusReturned;

        // The level offset is needed to find a nodes position in the traversal stack.
        // This is needed for parallel node which have their own stacks.
        private int _levelOffset;

        private Queue<int> _requestedTraversals = new Queue<int>();

        /// <summary>
        /// Called when the iterators finishes iterating the entire tree.
        /// </summary>
        public event Action OnDone = delegate { };

        public BehaviourIterator(BehaviourTree tree, int levelOffset)
        {
            _tree = tree;
            _traversal = new IntStack(_tree.Height);
            _levelOffset = levelOffset;
        }

        /// <summary>
        /// Ticks the iterator.
        /// </summary>
        public void Update()
        {
            callOnEnterOnQueuedNodes();

            int index = _traversal.Peek();
            BehaviourNode node = _tree.allNodes[index];
            _lastStatusReturned = node.Run();

#if UNITY_EDITOR
            node.SetStatusEditor(_lastStatusReturned);
#endif

            if (_lastStatusReturned != BehaviourNode.Status.Running) {

                node.OnExit();
                _traversal.Pop();
                callOnChildExit(node);
            }

            if (_traversal.Count == 0) {
                OnDone();
            }
        }

        private void callOnEnterOnQueuedNodes()
        {
            // Make sure to call on enter on any queued new traversals.
            while (_requestedTraversals.Count != 0) {

                int i = _requestedTraversals.Dequeue();

                BehaviourNode node = _tree.allNodes[i];
                node.OnEnter();

                callOnChildEnter(node);
            }
        }

        private void callOnChildEnter(BehaviourNode node)
        {
            if (node.Parent) {
                node.Parent.OnChildEnter(node._indexOrder);
            }
        }

        private void callOnChildExit(BehaviourNode node)
        {
            // If this is not a root node, then notify the parent about the child finishing.
            if (_traversal.Count > 0) {
                node.Parent.OnChildExit(node._indexOrder, _lastStatusReturned);
            }

            // If this was a subtree under a parallel node, then notify its parent.
            else if (node.Parent && _tree.IsParallelNode(node.Parent)) {
                node.Parent.OnChildExit(node._indexOrder, _lastStatusReturned);
            }
        }

        /// <summary>
        /// Requests the iterator to traverse a new node.
        /// </summary>
        /// <param name="next"></param>
        public void Traverse(BehaviourNode next)
        {
            int index = next.preOrderIndex;
            _traversal.Push(index);
            _requestedTraversals.Enqueue(index);

            _lastStatusReturned = BehaviourNode.Status.Running;

#if UNITY_EDITOR
            next.SetStatusEditor(BehaviourNode.Status.Running);
#endif
        }

        /// <summary>
        /// Tells the iterator to abort the current running subtree and jump to the aborter.
        /// </summary>
        /// <param name="aborter"></param>
        public void OnAbort(ConditionalAbort aborter)
        {
            BehaviourNode parent = aborter.Parent;
            int terminatingIndex = BehaviourNode.kInvalidOrder;

            if (parent) {
                terminatingIndex = parent.preOrderIndex;
            }

            // If an abort node is the root, then we need to empty the entire traversal.
            // We can achieve this by setting the terminating index to the invalid index, which is an invalid index
            // and will empty the traversal.
            while (_traversal.Peek() != terminatingIndex && _traversal.Count != 0) {
                stepBackAbort();
            }

            // Only composite nodes need to worry about which of their subtrees fired an abort.
            if (parent.MaxChildCount() > 1) {
                parent.OnAbort(aborter);
            }

            Traverse(aborter);
        }

        /// <summary>
        /// Gets the subtree that is running under a parent.
        /// This does not work directly under parallel nodes since they use their own iterator.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public BehaviourNode GetRunningSubtree(BehaviourNode parent)
        {
            int parentIndexInTraversal = GetIndexInTraversal(parent);
            int subtreeIndexInTraversal = parentIndexInTraversal + 1;

            int subtreePreOrder = _traversal.GetValue(subtreeIndexInTraversal);
            return _tree.allNodes[subtreePreOrder];
        }

        /// <summary>
        /// Gets the position of the node in the traversal stack.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public int GetIndexInTraversal(BehaviourNode node)
        {
            return  node.levelOrder - _levelOffset;
        }

        public bool IsRunning
        {
            get { return _traversal.Count != 0; }
        }

        /// <summary>
        /// Gets the pre-order index of the node at the top of the traversal stack.
        /// </summary>
        public int CurrentIndex
        {
            get { return _traversal.Peek(); }
        }

        public int LevelOffset
        {
            get { return _levelOffset; }
        }

        /// <summary>
        /// The last status stored by the iterator. Can be used by composites and decorators
        /// to find out what the child returned.
        /// </summary>
        public BehaviourNode.Status LastStatusReturned
        {
            get { return _lastStatusReturned; }
        }

        private void stepBackAbort()
        {
            int index = _traversal.Pop();

            BehaviourNode node = _tree.allNodes[index];
            node.OnExit();

#if UNITY_EDITOR
            node.SetStatusEditor(BehaviourNode.StatusEditor.Aborted);
#endif
        }

        /// <summary>
        /// Only interrupts the subtree until a parallel node.
        /// </summary>
        /// <param name="subtree"></param>
        internal void stepBackInterrupt(BehaviourNode subtree, bool bFullInterrupt = false)
        {
            while (_traversal.Count != 0 && _traversal.Peek() != subtree.preOrderIndex) {

                int index = _traversal.Pop();

                BehaviourNode node = _tree.allNodes[index];
                node.OnExit();
#if UNITY_EDITOR
                node.SetStatusEditor(BehaviourNode.StatusEditor.Interruption);
#endif

            }

            if (bFullInterrupt && _traversal.Count != 0) {
                int index = _traversal.Pop();

                BehaviourNode node = _tree.allNodes[index];
                node.OnExit();
#if UNITY_EDITOR
                node.SetStatusEditor(BehaviourNode.StatusEditor.Interruption);
#endif
            }
        }

        /// <summary>
        /// Gets the pre-order index of the node at the beginning of the traversal stack.
        /// </summary>
        public int FirstInTraversal
        {
            get { return _traversal.GetValue(0); }
        }

        /// <summary>
        /// A simple class to handle a custom stack structure with the 
        /// ability to query values in the middle of the stack.
        /// </summary>
        private class IntStack
        {
            private int[] _container;
            private int _count;

            public IntStack(int treeHeight)
            {
                // The tree height starts from zero
                // the the stack needs to have treeHeight + 1 slots.
                int maxDepth = treeHeight + 1;

                _count = 0;
                _container = new int[maxDepth];

                for (int i = 0; i < maxDepth; ++i) {
                    _container[i] = BehaviourNode.kInvalidOrder;
                }
            }

            public int Peek()
            {
                return _container[_count - 1];
            }

            public int Pop()
            {
                return _container[--_count];
            }

            public void Push(int value)
            {
                _container[_count++] = value;
            }

            public int Count
            {
                get { return _count; }
            }

            public int GetValue(int index)
            {
                return _container[index];
            }
        }
    }
}