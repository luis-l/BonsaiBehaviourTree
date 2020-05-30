
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

    struct Branch
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

    public override void OnEnter()
    {
      // Calculate the utility value of each branch.
      if (ChildCount() > 0)
      {
        Func<float, BehaviourNode, float> sumUtility = (accum, node) =>
        {
          return accum + node.UtilityValue();
        };

        _branchOrder = Enumerable
          .Range(0, ChildCount())
          .Select(index => new Branch(index, TreeIterator<BehaviourNode>.Traverse(GetChildAt(index), sumUtility, 0f)))
          .OrderByDescending(branch => branch.Utility)
          .ToArray();

        base.OnEnter();
      }
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