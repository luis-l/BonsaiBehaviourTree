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

    // The tree nodes in pre-order index.
    private readonly BehaviourNode[] nodes;

    private readonly Queue<int> requestedTraversals;

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

    /// <summary>
    /// The last status returned by an exiting child.
    /// Reset when nodes are entered.
    /// </summary>
    public BehaviourNode.Status? LastChildExitStatus { get; private set; }
    public BehaviourNode.Status LastExecutedStatus { get; private set; }

    /// <summary>
    /// Creates an iterator that will traverse the nodes.
    /// </summary>
    /// <param name="nodes">All the tree nodes in pre-order that can be traversed.</param>
    /// <param name="treeHeight">The max height of the tree branch that will be traversed.</param>
    public BehaviourIterator(BehaviourNode[] nodes, int treeHeight)
    {
      this.nodes = nodes;

      // Since tree heights starts from zero, the stack needs to have treeHeight + 1 slots.
      int maxTraversalLength = treeHeight + 1;
      traversal = new Utility.FixedSizeStack<int>(maxTraversalLength);
      requestedTraversals = new Queue<int>(maxTraversalLength);
    }

    /// <summary>
    /// Ticks the iterator.
    /// </summary>
    public void Update()
    {
      CallOnEnterOnQueuedNodes();
      int index = traversal.Peek();
      BehaviourNode node = nodes[index];
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
    }

    private void CallOnEnterOnQueuedNodes()
    {
      // Make sure to call on enter on any queued new traversals.
      while (requestedTraversals.Count != 0)
      {
        int i = requestedTraversals.Dequeue();
        BehaviourNode node = nodes[i];
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
      BehaviourNode node = nodes[index];

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