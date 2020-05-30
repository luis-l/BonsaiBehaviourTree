
using Bonsai.Core;
using Bonsai.Designer;

/// <summary>
/// Always returns running.
/// </summary>
[NodeEditorProperties("Tasks/", "Hourglass")]
public class Idle : Task
{

  public override Status Run()
  {
    return Status.Running;
  }
}
