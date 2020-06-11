
using System;
using UnityEngine;

namespace Bonsai.Designer
{
  public class BonsaiInputKnob : BonsaiKnob, IComparable<BonsaiInputKnob>
  {
    /// <summary>
    /// The output connected to the input.
    /// </summary>
    internal BonsaiOutputKnob outputConnection;

    public void OnDestroy()
    {
      // Since this input node got deleted, we need to notify
      // the output to forget about this node.
      if (outputConnection != null)
      {
        outputConnection.RemoveInputConnection(this);
      }
    }

    /// <summary>
    /// Tests the centers of the nodes associated and checks which node's center x-coord
    /// is lesser.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(BonsaiInputKnob other)
    {
      bool bIsLesser = parentNode.bodyRect.center.x < other.parentNode.bodyRect.center.x;

      return bIsLesser ? -1 : 1;
    }
  }
}