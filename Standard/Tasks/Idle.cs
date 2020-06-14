
using System.Text;
using Bonsai.Core;
using Bonsai.Designer;

/// <summary>
/// Always returns running.
/// </summary>
[BonsaiNode("Tasks/", "Hourglass")]
public class Idle : Task
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
