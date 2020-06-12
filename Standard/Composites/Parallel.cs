
using System.Linq;
using System.Collections.Generic;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Creates a basic parallel node which succeeds if all its children succeed.
  /// </summary>
  [BonsaiNode("Composites/", "ParallelArrows")]
  public class Parallel : Composite
  {
    // The iterators to run the children in sequential "parallel".
    protected List<BehaviourIterator> subIterators = new List<BehaviourIterator>();

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
        subIterators[i].Traverse(_children[i]);
      }
    }

    public override void OnExit()
    {
      for (int i = 0; i < subIterators.Count; ++i)
      {
        if (subIterators[i].IsRunning)
        {
          Tree.Interrupt(_children[i], true);
        }
      }
    }

    public override Status Run()
    {
      if (IsAnyChildWithStatus(Status.Failure))
      {
        return Status.Failure;
      }

      if (AreAllChildrenWithStatus(Status.Success))
      {
        return Status.Success;
      }

      // Process the sub-iterators.
      for (int i = 0; i < subIterators.Count; ++i)
      {
        // Keep updating the iterators that are not done.
        BehaviourIterator itr = subIterators[i];
        if (itr.IsRunning)
        {
          itr.Update();
        }
      }

      // Parallel iterators still running.
      return Status.Running;
    }

    public override void OnChildExit(int childIndex, Status childStatus)
    {
      ChildStatuses[childIndex] = childStatus;
    }

    public override void OnAbort(ConditionalAbort child)
    {
      // Do nothing. Parallel branches have same priority.
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
    public void SyncSubIterators()
    {
      // Set the new iterators. All of the sub-iterators have this parallel node as the root.
      // Offset the level order by +1 since the parallel parent is not included
      // in the subtree child iterator traversal stack.
      subIterators = Enumerable
        .Range(0, _children.Count)
        .Select(i => new BehaviourIterator(Tree, levelOrder + 1))
        .ToList();
    }

    public IEnumerable<BehaviourIterator> SubIterators
    {
      get { return subIterators; }
    }

    public BehaviourIterator GetIterator(int index)
    {
      return subIterators[index];
    }
  }
}