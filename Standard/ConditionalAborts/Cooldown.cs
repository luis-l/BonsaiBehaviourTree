
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;
using System.Text;

namespace Bonsai.Standard
{
  /// <summary>
  /// Locks the tree execution for a certain amount of time.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class Cooldown : ConditionalAbort
  {
    [Tooltip("The amount of time to wait at the cooldown decorator.")]
    public float cooldownTime = 1f;

    private readonly Utility.Timer timer = new Utility.Timer();

    public override void OnStart()
    {
      timer.OnTimeout += ResetConditionCache;
    }

    public override void OnEnter()
    {
      // We can only traverse the child if the cooldown is inactive.
      if (timer.IsDone)
      {
        _iterator.Traverse(_child);
      }
    }

    public override void OnExit()
    {
      timer.WaitTime = cooldownTime;
      timer.Start();
    }

    // Abort if the cooldown status changed.
    public override bool Condition()
    {
      return timer.IsDone;
    }

    public override Status Run()
    {
      // If the cooldown is active, fail to lock the branch from running.
      if (timer.IsRunning)
      {
        return Status.Failure;
      }

      // Cooldown is not active, pass the child branch status.
      return Iterator.LastStatusReturned;
    }

    public override void OnTreeTick()
    {
      if (!timer.IsDone)
      {
        timer.Update(Time.deltaTime);
      }
    }

    public override bool CanTickOnTree()
    {
      return true;
    }

    public override void StaticDescription(StringBuilder builder)
    {
      base.StaticDescription(builder);
      builder.AppendLine();
      builder.AppendFormat("Lock execution for {0:0.00}s", cooldownTime);
    }
  }
}