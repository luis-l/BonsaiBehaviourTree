
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
      // If a child failed then the sequence fails.
      if (_previousChildExitStatus == Status.Failure)
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