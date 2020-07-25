
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
      Status s = Iterator.LastChildExitStatus.GetValueOrDefault(Status.Success);
      return s == Status.Failure ? Status.Success : Status.Failure;
    }
  }
}