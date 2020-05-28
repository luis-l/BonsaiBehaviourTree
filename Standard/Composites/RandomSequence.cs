
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Composites/", "Shuffle")]
  public class RandomSequence : Sequence
  {
    private int[] _childrenOrder;

    [Tooltip("The seed for the random generator to use.")]
    public int seed;
    public bool useSeed;

    private System.Random rand;

    public override void OnStart()
    {
      int childCount = ChildCount();
      _childrenOrder = new int[childCount];

      // Fill in the orders with the default index order.
      for (int i = 0; i < childCount; ++i)
      {
        _childrenOrder[i] = i;
      }

      rand = useSeed ? new System.Random(seed) : new System.Random();
    }

    public override void OnEnter()
    {
      shuffleChildOrder();
      base.OnEnter();
    }

    public override BehaviourNode NextChild()
    {
      if (_currentChildIndex >= _childrenOrder.Length)
      {
        return null;
      }

      int index = _childrenOrder[_currentChildIndex];
      return _children[index];
    }

    private void shuffleChildOrder()
    {
      int childCount = ChildCount();

      for (int i = 0; i < childCount; i++)
      {

        int indexPivot = rand.Next(childCount);

        // Swap the i-th and pivot elements.
        int tmp = _childrenOrder[i];
        _childrenOrder[i] = _childrenOrder[indexPivot];
        _childrenOrder[indexPivot] = tmp;
      }
    }
  }
}