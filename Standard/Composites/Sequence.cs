
using Bonsai.Core;

namespace Bonsai.Standard
{
  /// <summary>
  /// Returns success if all children return success.
  /// </summary>
  [BonsaiNode("Composites/", "Arrow")]
  public class Sequence : Composite
  {
    public override Status Run()
    {
      // If a child failed then the sequence fails.
      if (lastChildExitStatus == Status.Failure)
      {
        return Status.Failure;
      }

      // Else child returned success.

      // Get the next child
      var nextChild = CurrentChild();

      // If this was the last child then the sequence returns success.
      if (nextChild == null)
      {
        return Status.Success;
      }

      // Still need children to process.
      else
      {
        Iterator.Traverse(nextChild);
        return Status.Running;
      }
    }
  }
}