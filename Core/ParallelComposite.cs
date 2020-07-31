using System.Linq;

namespace Bonsai.Core
{
  /// <summary>
  /// Provides functionality to execute child branches in parallel.
  /// </summary>
  public abstract class ParallelComposite : Composite
  {
    // The iterators that will run branches concurrently.
    public BehaviourIterator[] BranchIterators { get; private set; }

    protected Status[] ChildStatuses { get; private set; }

    public sealed override void OnStart()
    {
      int count = ChildCount();
      ChildStatuses = new Status[count];

      // Set the branch iterators. Branch iterators have this parallel node as their root.
      // Offset level order by +1 since the parallel parent is not included in branch traversal.
      int childLevelOrder = levelOrder + 1;
      int maxSubtreeHeight = Tree.Height - childLevelOrder; // max depth of child branches.
      BranchIterators = Enumerable
        .Range(0, count)
        .Select(i => new BehaviourIterator(Tree.Nodes, maxSubtreeHeight))
        .ToArray();

      // Assign the branch iterator to nodes not under any parallel nodes.
      // Children under parallel nodes will have iterators assigned by the local parallel parent.
      // Each branch under a parallel node use their own branch iterator.
      for (int i = 0; i < count; i++)
      {
        BehaviourIterator branchIterator = BranchIterators[i];
        foreach (BehaviourNode node in TreeTraversal.PreOrderSkipChildren(GetChildAt(i), n => n is ParallelComposite))
        {
          node.Iterator = branchIterator;
        }
      }
    }

    public sealed override void OnEnter()
    {
      // Traverse children at the same time.
      for (int i = 0; i < Children.Length; ++i)
      {
        ChildStatuses[i] = Status.Running;
        BranchIterators[i].Traverse(Children[i]);
      }
    }

    public sealed override void OnExit()
    {
      for (int i = 0; i < BranchIterators.Length; ++i)
      {
        if (BranchIterators[i].IsRunning)
        {
          BehaviourTree.Interrupt(Children[i]);
        }
      }
    }

    public sealed override void OnChildExit(int childIndex, Status childStatus)
    {
      ChildStatuses[childIndex] = childStatus;
    }

    public sealed override void OnAbort(int childIndex)
    {
      // Do nothing. Parallel branches have same priority.
    }

    protected void RunChildBranches()
    {
      foreach (BehaviourIterator i in BranchIterators)
      {
        if (i.IsRunning)
        {
          i.Update();
        }
      }
    }

    protected bool IsAnyChildWithStatus(Status expected)
    {
      foreach (var status in ChildStatuses)
      {
        if (status == expected)
        {
          return true;
        }
      }

      return false;
    }

    protected bool AreAllChildrenWithStatus(Status expected)
    {
      foreach (var status in ChildStatuses)
      {
        if (status != expected)
        {
          return false;

        }
      }

      return true;
    }
  }
}
