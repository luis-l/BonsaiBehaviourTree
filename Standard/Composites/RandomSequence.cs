
using Bonsai.Core;

namespace Bonsai.Standard
{
  [BonsaiNode("Composites/", "Shuffle")]
  public class RandomSequence : Sequence
  {
    private int[] branchOrder;

    public override void OnStart()
    {
      int childCount = ChildCount();
      branchOrder = new int[childCount];

      // Fill in the orders with the default index order.
      for (int i = 0; i < childCount; ++i)
      {
        branchOrder[i] = i;
      }
    }

    public override void OnEnter()
    {
      ShuffleChildOrder();
      base.OnEnter();
    }

    public override BehaviourNode CurrentChild()
    {
      if (CurrentChildIndex >= branchOrder.Length)
      {
        return null;
      }

      int index = branchOrder[CurrentChildIndex];
      return Children[index];
    }

    private void ShuffleChildOrder()
    {
      int childCount = ChildCount();

      for (int i = 0; i < childCount; i++)
      {
        int indexPivot = UnityEngine.Random.Range(0, childCount);

        // Swap the i-th and pivot elements.
        int tmp = branchOrder[i];
        branchOrder[i] = branchOrder[indexPivot];
        branchOrder[indexPivot] = tmp;
      }
    }
  }
}