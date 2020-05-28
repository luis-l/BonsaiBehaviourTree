using System;
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

    // How many iterators finished their traversal.
    protected int _subIteratorsDoneCount;

    public override void OnEnter()
    {
      _subIteratorsDoneCount = 0;

      // Traverse children at the same time.
      for (int childIndex = 0; childIndex < _children.Count; ++childIndex)
      {

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

    public override BehaviourNode.Status Run()
    {
      // All iterators done.
      // Since there was no failure interruption, that means
      // all iterators returned success, so the parallel node
      // returns success aswell.
      if (IsDone)
      {
        return BehaviourNode.Status.Success;
      }

      // Process the sub-iterators.
      for (int i = 0; i < _subIterators.Count; ++i)
      {

        BehaviourIterator itr = _subIterators[i];

        // Keep updating the iterators that are not done.
        if (itr.IsRunning)
        {
          itr.Update();
        }

        // Iterator finished, it must have returned Success or Failure.
        // If the iterator returned failure, then interrupt the parallel process
        // and have the parallel node return failure.
        else if (itr.LastStatusReturned == BehaviourNode.Status.Failure)
        {
          return Status.Failure;
        }
      }

      // Parallel iterators still running.
      return Status.Running;
    }

    /// <summary>
    /// Tests to see if all the sub-iterators finished traversing their child subtree.
    /// </summary>
    public bool IsDone
    {
      get { return _subIteratorsDoneCount == _subIterators.Count; }
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
        sub.OnDone += incrementDone;

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

    private void incrementDone()
    {
      ++_subIteratorsDoneCount;
    }
  }
}