
using System.Text;
using Bonsai.Core;

namespace Bonsai.Standard
{
  /// <summary>
  /// Alaways returns failure.
  /// </summary>
  [BonsaiNode("Decorators/", "SmallCross")]
  public class Failure : Decorator
  {
    public override Status Run()
    {
      return Status.Failure;
    }

    public override void Description(StringBuilder builder)
    {
      builder.Append("Always fail");
    }
  }
}