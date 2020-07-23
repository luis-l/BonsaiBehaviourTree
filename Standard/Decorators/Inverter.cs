
using Bonsai.Core;

namespace Bonsai.Standard
{
  /// <summary>
  /// Negates the status of the child.
  /// </summary>
  [BonsaiNode("Decorators/", "Exclamation")]
  public class Inverter : Decorator
  {
    public override Status Run()
    {
      Status s = Iterator.LastStatusReturned;

      if (s == Status.Failure)
        return Status.Success;

      return Status.Failure;
    }
  }
}