
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

    private struct Branch
    {
      public Branch(int index, float utility)
      {
        Index = index;
        Utility = utility;
      }

      public int Index { get; set; }
      public float Utility { get; set; }
    }

    private Branch[] _branchOrder;

    public override void OnStart()
    {
      _branchOrder = Enumerable.Range(0, ChildCount()).Select(index => new Branch(index, 0f)).ToArray();
    }

    public override void OnEnter()
    {
      // Calculate the utility value of each branch.
      if (ChildCount() > 0)
      {
        for (int i = 0; i < ChildCount(); i++)
        {
          _branchOrder[i].Utility = TreeIterator<BehaviourNode>.Traverse(GetChildAt(i), sumUtility, 0f);
        }

        Array.Sort(_branchOrder, descendingUtilityOrder);

        base.OnEnter();
      }
    }

    private static float sumUtility(float accumulatedUtility, BehaviourNode node)
    {
      return accumulatedUtility + node.UtilityValue();
    }


    private static int descendingUtilityOrder(Branch left, Branch right)
    {
      return -left.Utility.CompareTo(right.Utility);
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
  }
}