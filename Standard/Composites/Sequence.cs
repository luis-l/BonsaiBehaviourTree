
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
      if (status == Status.Failure)
      {
        return Status.Failure;
      }

      // Else child returned success.

      // Get the next child
      var nextChild = NextChild();

      // If this was the last child then the sequence returns success.
      if (nextChild == null)
      {
        return Status.Success;
      }

      // Still need children to process.
      else
      {
        _iterator.Traverse(nextChild);
        return Status.Running;
      }
    }
  }
}