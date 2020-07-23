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

    public override void OnStart()
    {
      ChildStatuses = new Status[ChildCount()];
    }

    public override void OnEnter()
    {
      // Traverse children at the same time.
      for (int i = 0; i < _children.Count; ++i)
      {
        ChildStatuses[i] = Status.Running;
        BranchIterators[i].Traverse(_children[i]);
      }
    }

    public override void OnExit()
    {
      for (int i = 0; i < BranchIterators.Length; ++i)
      {
        if (BranchIterators[i].IsRunning)
        {
          Tree.Interrupt(_children[i], true);
        }
      }
    }

    public override void OnChildExit(int childIndex, Status childStatus)
    {
      ChildStatuses[childIndex] = childStatus;
    }

    public override void OnAbort(ConditionalAbort child)
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

    /// <summary>
    /// Sets the number of sub-iterators to the number of children.
    /// </summary>
    internal void SyncSubIterators()
    {
      // Set the new iterators. All of the sub-iterators have this parallel node as the root.
      // Offset the level order by +1 since the parallel parent is not included
      // in the subtree child iterator traversal stack.
      BranchIterators = Enumerable
        .Range(0, _children.Count)
        .Select(i => new BehaviourIterator(Tree, levelOrder + 1))
        .ToArray();
    }
  }
}
