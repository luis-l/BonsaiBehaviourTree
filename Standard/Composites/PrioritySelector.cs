
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

    public override void OnStart()
    {
      _branchOrder = Enumerable.Range(0, ChildCount()).Select(index => new Branch(index, 0f)).ToArray();
    }

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
        for (int i = 0; i < ChildCount(); i++)
        {
          _branchOrder[i].Priority = TreeIterator<BehaviourNode>.Traverse(GetChildAt(i), maxPriority, 0f);
        }

        Array.Sort(_branchOrder, descendingPriorityOrder);
      }
    }

    private static float maxPriority(float maxSoFar, BehaviourNode node)
    {
      return Math.Max(maxSoFar, node.Priority());
    }

    private static int descendingPriorityOrder(Branch left, Branch right)
    {
      return -left.Priority.CompareTo(right.Priority);
    }
  }
}