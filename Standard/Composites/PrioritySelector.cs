
using System;
using System.Linq;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Composites/", "Priority")]
  public class PrioritySelector : Selector
  {
    struct Branch
    {
      public Branch(int index, float priority)
      {
        Index = index;
        Priority = priority;
      }

      public int Index { get; set; }
      public float Priority { get; set; }
    }

    // The indices of the children in priority order.
    private Branch[] _branchOrder;

    // Order the child priorities
    public override void OnEnter()
    {
      sortPriorities();
      base.OnEnter();
    }

    // The order of the children is from highest to lowest priority.
    public override BehaviourNode NextChild()
    {
      if (_currentChildIndex >= _branchOrder.Length)
      {
        return null;
      }

      int index = _branchOrder[_currentChildIndex].Index;
      return _children[index];
    }

    private void sortPriorities()
    {
      // Calculate the utility value of each branch.
      if (ChildCount() > 0)
      {
        Func<float, BehaviourNode, float> maxPriority = (accum, node) =>
        {
          return Math.Max(accum, node.Priority());
        };

        _branchOrder = Enumerable
          .Range(0, ChildCount())
          .Select(index => new Branch(index, TreeIterator<BehaviourNode>.Traverse(GetChildAt(index), maxPriority, int.MinValue)))
          .OrderByDescending(branch => branch.Priority)
          .ToArray();
      }
    }
  }
}