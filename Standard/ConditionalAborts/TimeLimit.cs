
using System.Text;
using Bonsai.Core;
using Bonsai.Designer;
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

    private float counter = 0f;

    protected override void OnEnable()
    {
      abortType = AbortType.Self;
      base.OnEnable();
    }

    public override void OnEnter()
    {
      counter = 0f;
      base.OnEnter();
    }

    public override bool Condition()
    {
      return counter < timeLimit;
    }

    public override void OnBranchTick()
    {
      counter += Time.deltaTime;
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