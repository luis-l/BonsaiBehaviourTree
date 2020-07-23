
using System.Text;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
  /// <summary>
  /// Sets a time limit for the child to finish executing.
  /// If the time is up, the decorator returns failure.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class TimeLimit : ConditionalAbort
  {
    public float timeLimit = 1f;

    [ShowAtRuntime]
    private float timer = 0f;

    void OnEnable()
    {
      abortType = AbortType.Self;
    }

    public override void OnEnter()
    {
      timer = 0f;
      base.OnEnter();
    }

    public override bool Condition()
    {
      return timer < timeLimit;
    }

    public override void OnBranchTick()
    {
      timer += Time.deltaTime;
    }

    public override bool CanTickOnBranch()
    {
      // Enable branch ticking so we can update the timer.
      return true;
    }

    public override void Description(StringBuilder builder)
    {
      base.Description(builder);
      builder.AppendLine();
      builder.AppendFormat("Abort and fail after {0:0.00}s", timeLimit);
    }
  }
}