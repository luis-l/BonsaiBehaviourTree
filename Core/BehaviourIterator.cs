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
    private readonly Utility.FixedSizeStack<int> traversal;

    // Access to the tree so we can find any node from pre-order index.
    private readonly BehaviourTree tree;
    private readonly Queue<int> requestedTraversals;

    /// <summary>
    /// Called when the iterators finishes iterating the entire tree.
    /// </summary>
    public Action OnDone = delegate { };

    public bool IsRunning
    {
      get { return traversal.Count != 0; }
    }

    /// <summary>
    /// Gets the pre-order index of the node at the top of the traversal stack.
    /// If the iterator is not traversing anything, -1 is returned.
    /// </summary>
    public int CurrentIndex
    {
      get { return traversal.Count == 0 ? BehaviourNode.kInvalidOrder : traversal.Peek(); }
    }

    public int LevelOffset { get; }

    /// <summary>
    /// The last status returned by an exiting child.
    /// Reset when nodes are entered.
    /// </summary>
    public BehaviourNode.Status? LastChildExitStatus { get; private set; }
    public BehaviourNode.Status LastExecutedStatus { get; private set; }

    /// <summary>
    /// Gets the pre-order index of the node at the beginning of the traversal stack.
    /// </summary>
    public int FirstInTraversal
    {
      get { return traversal.GetValue(0); }
    }

    public BehaviourIterator(BehaviourTree tree, int levelOffset)
    {
      this.tree = tree;

      // Since tree heights starts from zero, the stack needs to have treeHeight + 1 slots.
      int maxTraversalLength = this.tree.Height + 1;
      traversal = new Utility.FixedSizeStack<int>(maxTraversalLength);
      requestedTraversals = new Queue<int>(maxTraversalLength);

      LevelOffset = levelOffset;
    }

    /// <summary>
    /// Ticks the iterator.
    /// </summary>
    public void Update()
    {
      CallOnEnterOnQueuedNodes();
      int index = traversal.Peek();
      BehaviourNode node = tree.Nodes[index];
      BehaviourNode.Status s = node.Run();

      LastExecutedStatus = s;

#if UNITY_EDITOR
      node.StatusEditorResult = (BehaviourNode.StatusEditor)s;
#endif

      if (s != BehaviourNode.Status.Running)
      {
        PopNode();
        OnChildExit(node, s);
      }

      if (traversal.Count == 0)
      {
        OnDone();
      }
    }

    private void CallOnEnterOnQueuedNodes()
    {
      // Make sure to call on enter on any queued new traversals.
      while (requestedTraversals.Count != 0)
      {
        int i = requestedTraversals.Dequeue();
        BehaviourNode node = tree.Nodes[i];
        node.OnEnter();
        OnChildEnter(node);
      }
    }

    private void OnChildEnter(BehaviourNode node)
    {
      if (node.Parent)
      {
        LastChildExitStatus = null;
        node.Parent.OnChildEnter(node.indexOrder);
      }
    }

    private void OnChildExit(BehaviourNode node, BehaviourNode.Status s)
    {
      if (node.Parent)
      {
        node.Parent.OnChildExit(node.indexOrder, s);
        LastChildExitStatus = s;
      }
    }

    /// <summary>
    /// Requests the iterator to traverse a new node.
    /// </summary>
    /// <param name="next"></param>
    public void Traverse(BehaviourNode next)
    {
      int index = next.preOrderIndex;
      traversal.Push(index);
      requestedTraversals.Enqueue(index);

#if UNITY_EDITOR
      next.StatusEditorResult = BehaviourNode.StatusEditor.Running;
#endif
    }

    /// <summary>
    /// Tells the iterator to abort the current running branch and jump to the aborter.
    /// </summary>
    /// <param name="parent">The parent that will abort is running branch.</param>
    /// <param name="abortBranchIndex">The child branch that caused the abort.</param>
    public void AbortRunningChildBranch(BehaviourNode parent, int abortBranchIndex)
    {
      // If the iterator is inactive, ignore.
      if (IsRunning && parent)
      {
        int terminatingIndex = parent.preOrderIndex;

        while (traversal.Count != 0 && traversal.Peek() != terminatingIndex)
        {
          StepBackAbort();
        }

        // Only composite nodes need to worry about which of their subtrees fired an abort.
        if (parent.IsComposite())
        {
          parent.OnAbort(abortBranchIndex);
        }

        // Any requested traversals are cancelled on abort.
        requestedTraversals.Clear();

        Traverse(parent.GetChildAt(abortBranchIndex));
      }
    }

    // Do a single step abort.
    private void StepBackAbort()
    {
      var node = PopNode();

#if UNITY_EDITOR
      node.StatusEditorResult = BehaviourNode.StatusEditor.Aborted;
#endif
    }

    /// <summary>
    /// Interrupts the subtree traversed by the iterator.
    /// </summary>
    /// <param name="subtree"></param>
    internal void Interrupt(BehaviourNode subtree)
    {
      // Keep interrupting up to the parent of subtree. 
      // The parent is not interrupted; subtree node is interrupted.
      if (subtree)
      {
        int parentIndex = subtree.Parent ? subtree.Parent.PreOrderIndex : BehaviourNode.kInvalidOrder;
        while (traversal.Count != 0 && traversal.Peek() != parentIndex)
        {
          var node = PopNode();

#if UNITY_EDITOR
          node.StatusEditorResult = BehaviourNode.StatusEditor.Interruption;
#endif
        }

        // Any requested traversals are cancelled on interruption.
        requestedTraversals.Clear();
      }
    }

    private BehaviourNode PopNode()
    {
      int index = traversal.Pop();
      BehaviourNode node = tree.Nodes[index];

      if (node.IsComposite())
      {
        for (int i = 0; i < node.ChildCount(); i++)
        {
          node.GetChildAt(i).OnCompositeParentExit();
        }
      }

      node.OnExit();
      return node;
    }
  }
}