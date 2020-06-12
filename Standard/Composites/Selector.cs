
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Returns success if one child returns success.
  /// </summary>
  [BonsaiNode("Composites/", "Question")]
  public class Selector : Composite
  {
    public override Status Run()
    {
      // If a child succeeded then the selector succeeds.
      if (_previousChildExitStatus == Status.Success)
      {
        return Status.Success;
      }

      // Else child returned failure.

      // Get the next child
      var nextChild = NextChild();

      // If this was the last child then the selector fails.
      if (nextChild == null)
      {
        return Status.Failure;
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