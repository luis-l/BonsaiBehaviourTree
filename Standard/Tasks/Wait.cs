
using System.Text;
using Bonsai.Core;

namespace Bonsai.Standard
{
  [BonsaiNode("Tasks/", "Timer")]
  public class Wait : Task
  {
    [ShowAtRuntime]
    [UnityEngine.SerializeField]
    public Utility.Timer timer = new Utility.Timer();

    public override void OnEnter()
    {
      timer.Start();
    }

    public override Status Run()
    {
      timer.Update(UnityEngine.Time.deltaTime);
      return timer.IsDone ? Status.Success : Status.Running;
    }

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("Wait for {0:0.00}s", timer.interval);
    }
  }
}