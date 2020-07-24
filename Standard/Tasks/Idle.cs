
using System.Text;

namespace Bonsai.Standard
{
  /// <summary>
  /// Always returns running.
  /// </summary>
  [BonsaiNode("Tasks/", "Hourglass")]
  public class Idle : Core.Task
  {
    public override Status Run()
    {
      return Status.Running;
    }

    public override void Description(StringBuilder builder)
    {
      builder.Append("Run forever");
    }
  }
}

