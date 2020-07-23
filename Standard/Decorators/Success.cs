
using System.Text;
using Bonsai.Core;

namespace Bonsai.Standard
{
  /// <summary>
  /// Always returns success.
  /// </summary>
  [BonsaiNode("Decorators/", "SmallCheckmark")]
  public class Success : Decorator
  {
    public override Status Run()
    {
      return Status.Success;
    }

    public override void Description(StringBuilder builder)
    {
      builder.Append("Always succeed");
    }
  }
}