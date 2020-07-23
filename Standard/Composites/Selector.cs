
using Bonsai.Core;

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
      if (lastChildExitStatus == Status.Success)
      {
        return Status.Success;
      }

      // Else child returned failure.

      // Get the next child
      var nextChild = CurrentChild();

      // If this was the last child then the selector fails.
      if (nextChild == null)
      {
        return Status.Failure;
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