
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Sets a time limit for the child to finish executing.
  /// If the time is up, the decorator returns failure.
  /// </summary>
  [NodeEditorProperties("Conditional/", "Condition")]
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
  }
}