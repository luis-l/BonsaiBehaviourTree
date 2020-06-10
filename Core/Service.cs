
using UnityEngine;
using System;

namespace Bonsai.Core
{
  public abstract class Service : Decorator
  {
    public float interval = 0.5f;
    public float randomDeviation = 0f;
    public bool restartTimerOnEnter = true;

    private readonly Utility.Timer timer = new Utility.Timer();

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
  }

}
