
using System;
using System.Linq;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Composites/", "Priority")]
  public class PrioritySelector : Selector
  {
    struct Branch : IComparable<Branch>
    {
      public Branch(int index, float priority)
      {
        Index = index;
        Priority = priority;
      }

      public int Index { get; set; }
      public float Priority { get; set; }

      public int CompareTo(Branch other)
      {
        // Negate for descending order.
        return -Priority.CompareTo(other.Priority);
      }
    }

    // The indices of the children in priority order.
    private Branch[] _branchOrder;
    private TreeQueryIterator _priorityQueryIterator;
    private Utility.FixedSorter<Branch> _prioritySorter;

    public override void OnStart()
    {
      _branchOrder = Enumerable.Range(0, ChildCount()).Select(index => new Branch(index, 0f)).ToArray();
      _priorityQueryIterator = new TreeQueryIterator(Tree.Height - LevelOrder);
      _prioritySorter = new Utility.FixedSorter<Branch>(_branchOrder);
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
      // Calculate the priority value of each branch.
      if (ChildCount() > 0)
      {
        for (int i = 0; i < ChildCount(); i++)
        {
          _branchOrder[i].Priority = _priorityQueryIterator.MaxPriority(GetChildAt(i));
        }
        _prioritySorter.Sort();
      }
    }

  }
}