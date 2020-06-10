
using System.Collections.Generic;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Creates a basic parallel node which succeeds if all its children succeed.
  /// </summary>
  [NodeEditorProperties("Composites/", "ParallelArrows")]
  public class Parallel : Composite
  {
    // The iterators to run the children in sequential "parallel".
    protected List<BehaviourIterator> _subIterators = new List<BehaviourIterator>();

    protected Status[] ChildStatuses { get; private set; }

    public override void OnStart()
    {
      ChildStatuses = new Status[ChildCount()];
    }

    public override void OnEnter()
    {
      // Traverse children at the same time.
      for (int childIndex = 0; childIndex < _children.Count; ++childIndex)
      {
        ChildStatuses[childIndex] = Status.Running;
        var child = _children[childIndex];
        _subIterators[childIndex].Traverse(child);
      }
    }

    public override void OnExit()
    {
      for (int i = 0; i < _subIterators.Count; ++i)
      {

        BehaviourIterator itr = _subIterators[i];

        if (itr.IsRunning)
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
      for (int i = 0; i < _subIterators.Count; ++i)
      {

        // Keep updating the iterators that are not done.
        BehaviourIterator itr = _subIterators[i];
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
      _subIterators = new List<BehaviourIterator>(_children.Count);

      // Set the new iterators. All of the sub-iterators have this parallel node as the root.
      foreach (var child in _children)
      {

        // Offset the level order by +1 since the parallel parent is not included
        // in the subtree child iterator traversal stack.
        var sub = new BehaviourIterator(Tree, levelOrder + 1);
        _subIterators.Add(sub);
      }
    }

    public IEnumerable<BehaviourIterator> SubIterators
    {
      get { return _subIterators; }
    }

    public BehaviourIterator GetIterator(int index)
    {
      return _subIterators[index];
    }
  }
}