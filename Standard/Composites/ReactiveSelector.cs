
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Reacts to changes in children statuses and will run the highest priority child that returns a running status. 
  /// The highest priority running child will interrupt any lower priority running children.
  /// </summary>
  [NodeEditorProperties("Composites/", "Reactive")]
  public class ReactiveSelector : Parallel
  {
    private int _childCount;
    private int _failureCount;
    private Status[] _childStatuses;

    public override void OnStart()
    {
      _childCount = ChildCount();
      _childStatuses = new Status[_childCount];
    }

    public override void OnEnter()
    {
      // Do not traverse the children immediately.
      _currentChildIndex = BehaviourNode.kInvalidOrder;
    }

    public override Status Run()
    {
      // Since the reactive selector has preference for higher priority children (left to right)
      // Then we only need to check up to a certain range.
      //
      // If there are no running children, then we must check all the children.
      int reevaluationRange = _childCount;

      // There is a running node.
      if (_currentChildIndex != BehaviourNode.kInvalidOrder)
      {

        // If this child is running, then the next re-evaluation only needs
        // to check children up this index.
        reevaluationRange = _currentChildIndex;

        _subIterators[_currentChildIndex].Update();

        Status s = _childStatuses[_currentChildIndex];

        if (s == Status.Success)
        {
          return Status.Success;
        }

        else if (s == Status.Failure)
        {

          // The current index is now invalid since there are no running children,
          _currentChildIndex = BehaviourNode.kInvalidOrder;

          // We must then search all the children in the next re-evaluation.
          reevaluationRange = _childCount;

          // Count up the failures in order to fail the parent node if this was the last node.
          _failureCount++;
        }
      }

      // All children failed.
      if (_failureCount == _childCount)
      {
        return Status.Failure;
      }

      // Reset the fail count in order to recount failures in the next re-evaluation.
      _failureCount = 0;

      // Re-evaluate the children
      for (int i = 0; i < reevaluationRange; ++i)
      {

        // Traverse the child and update to see if we should 
        // keep this child as running or look for another child.
        _childStatuses[i] = Status.Running;
        _subIterators[i].Traverse(_children[i]);
        _subIterators[i].Update();

        // Keep this child.
        if (_childStatuses[i] == Status.Running)
        {

          // Change in running child. Interrupt the old running child.
          if (_currentChildIndex != BehaviourNode.kInvalidOrder && _currentChildIndex != i)
          {
            Tree.Interrupt(_children[_currentChildIndex], true);
          }

          // Update the current running child to this child.
          _currentChildIndex = i;
          break;
        }

        // Success found
        else if (_childStatuses[i] == Status.Success)
        {
          return Status.Success;
        }

        // Failure encountered, keep track of how many failed so we know when to abort.
        else
        {
          _failureCount++;
        }
      }

      return Status.Running;
    }

    protected internal override void OnChildExit(int childIndex, BehaviourNode.Status childStatus)
    {
      _childStatuses[childIndex] = childStatus;
    }
  }
}