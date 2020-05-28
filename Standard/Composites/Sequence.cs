using System;
using System.Collections.Generic;
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Returns success if all children return success.
  /// </summary>
  [NodeEditorProperties("Composites/", "Arrow")]
  public class Sequence : Composite
  {
    public override Status Run()
    {
      // A parent will only receive a Success or Failure, never a Running.
      var status = _iterator.LastStatusReturned;

      // If a child failed then the sequence fails.
      if (status == BehaviourNode.Status.Failure)
      {
        return BehaviourNode.Status.Failure;
      }

      // Else child returned success.

      // Get the next child
      var nextChild = NextChild();

      // If this was the last child then the sequence returns success.
      if (nextChild == null)
      {
        return BehaviourNode.Status.Success;
      }

      // Still need children to process.
      else
      {
        _iterator.Traverse(nextChild);
        return BehaviourNode.Status.Running;
      }
    }
  }
}