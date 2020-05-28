
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Negates the status of the child.
  /// </summary>
  [NodeEditorProperties("Decorators/", "Exclamation")]
  public class Inverter : Decorator
  {
    public override Status Run()
    {
      Status s = _iterator.LastStatusReturned;

      if (s == Status.Failure)
        return Status.Success;

      return Status.Failure;
    }
  }
}