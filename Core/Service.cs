
using System.Text;

namespace Bonsai.Core
{
  public abstract class Service : Decorator
  {
    public bool restartTimerOnEnter = true;

    [ShowAtRuntime]
    [UnityEngine.SerializeField]
    public Utility.Timer timer = new Utility.Timer();

    public sealed override void OnStart()
    {
      timer.OnTimeout += ServiceTick;
      timer.AutoRestart = true;
    }

    public sealed override void OnEnter()
    {
      Tree.AddTimer(timer);

      if (timer.IsDone || restartTimerOnEnter)
      {
        timer.Start();
      }

      base.OnEnter();
    }

    public sealed override void OnExit()
    {
      Tree.RemoveTimer(timer);
    }

    public sealed override Status Run()
    {
      // Simply pass the status.
      return Iterator.LastChildExitStatus.GetValueOrDefault(Status.Failure);
    }

    /// <summary>
    /// Apply changes by the service
    /// </summary>
    protected abstract void ServiceTick();

    public override void Description(StringBuilder builder)
    {
      if (timer.deviation == 0f)
      {
        builder.AppendFormat("Tick {0:0.00}s", timer.interval);
      }
      else
      {
        float lower = timer.interval - timer.deviation;
        float upper = timer.interval + timer.deviation;
        builder.AppendFormat("Tick {0:0.00}s - {1:0.00}s", lower, upper);
      }

      builder.AppendLine();
      builder.Append(restartTimerOnEnter ? "Restart timer on enter" : "Resume timer on enter");
    }
  }

}
