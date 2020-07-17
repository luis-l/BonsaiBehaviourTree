
using UnityEngine;
using System;
using System.Text;

namespace Bonsai.Core
{
  public abstract class Service : Decorator
  {
    public float interval = 0.5f;
    public float randomDeviation = 0f;
    public bool restartTimerOnEnter = true;

    [Designer.ShowAtRuntime]
    protected readonly Utility.Timer timer = new Utility.Timer();

    public Action OnEvaluation = delegate { };

    public sealed override void OnStart()
    {
      timer.OnTimeout += ServiceTick;
      timer.AutoRestart = true;
    }

    public sealed override void OnEnter()
    {
      UpdateWaitTime();

      if (timer.IsDone || restartTimerOnEnter)
      {
        timer.Start();
      }

      base.OnEnter();
    }

    public sealed override Status Run()
    {
      // Simply pass the status.
      return Iterator.LastStatusReturned;
    }

    public sealed override bool CanTickOnBranch()
    {
      return true;
    }

    /// <summary>
    /// Ticks the service.
    /// </summary>
    public sealed override void OnBranchTick()
    {
      timer.Update(Time.deltaTime);
    }

    /// <summary>
    /// Apply changes by the service
    /// </summary>
    protected abstract void ServiceTick();

    private void UpdateWaitTime()
    {
      timer.WaitTime = interval + ((float)Tree.Random.NextDouble() * 2f * randomDeviation - randomDeviation);
    }

    public override void Description(StringBuilder builder)
    {
      if (randomDeviation == 0)
      {
        builder.AppendFormat("Tick {0:0.00}s", interval);
      }
      else
      {
        float lower = interval - randomDeviation;
        float upper = interval + randomDeviation;
        builder.AppendFormat("Tick {0:0.00}s - {1:0.00}s", lower, upper);
      }

      builder.AppendLine();
      builder.Append(restartTimerOnEnter ? "Restart timer on enter" : "Resume timer on enter");
    }
  }

}
