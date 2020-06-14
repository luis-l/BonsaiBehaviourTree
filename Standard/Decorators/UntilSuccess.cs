
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Keep re-traversing children until the child return success.
  /// </summary>
  [BonsaiNode("Decorators/", "RepeatArrow")]
  public class UntilSuccess : Decorator
  {
    public override Status Run()
    {
      Status s = _iterator.LastStatusReturned;

      if (s == Status.Success)
      {
        return Status.Success;
      }

      // Retraverse child.
      _iterator.Traverse(_child);

      return Status.Running;
    }
  }
}