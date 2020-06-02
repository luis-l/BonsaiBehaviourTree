
using System;
using System.Linq;
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Runs the branch that has the highest utility.
  /// </summary>
  [NodeEditorProperties("Composites/", "Play")]
  public class UtilitySelector : Selector
  {

    private struct Branch : IComparable<Branch>
    {
      public Branch(int index, float utility)
      {
        Index = index;
        Utility = utility;
      }

      public int Index { get; set; }
      public float Utility { get; set; }

      public int CompareTo(Branch other)
      {
        // Negate for descending order.
        return -Utility.CompareTo(other.Utility);
      }
    }

    private Branch[] _branchOrder;
    private TreeQueryIterator _utilityQueryIterator;
    private Utility.FixedSorter<Branch> _utilitySorter;

    public override void OnStart()
    {
      _branchOrder = Enumerable.Range(0, ChildCount()).Select(index => new Branch(index, 0f)).ToArray();
      _utilityQueryIterator = new TreeQueryIterator(Tree.Height - LevelOrder);
      _utilitySorter = new Utility.FixedSorter<Branch>(_branchOrder);
    }

    public override void OnEnter()
    {
      sortUtilities();
      base.OnEnter();
    }

    // Get children by utility order.
    public override BehaviourNode NextChild()
    {
      if (_currentChildIndex >= _branchOrder.Length)
      {
        return null;
      }

      int index = _branchOrder[_currentChildIndex].Index;
      return _children[index];
    }

    private void sortUtilities()
    {
      // Calculate the utility value of each branch.
      if (ChildCount() > 0)
      {
        for (int i = 0; i < ChildCount(); i++)
        {
          _branchOrder[i].Utility = _utilityQueryIterator.SumUtility(GetChildAt(i));
        }
        _utilitySorter.Sort();
      }
    }

  }
}