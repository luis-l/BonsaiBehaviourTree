
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Runs the branch that has the highest utility.
  /// </summary>
  [NodeEditorProperties("Composites/", "Play")]
  public class UtilitySelector : Selector
  {
    public override void OnStart()
    {
    }

    public override void OnEnter()
    {
    }

    public override Status Run()
    {
      return Status.Running;
    }

    protected internal override void OnChildExit(int childIndex, Status childStatus)
    {
    }
  }
}