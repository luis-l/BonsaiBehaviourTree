
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
    private float _counter = 0f;
    private bool _bChildRunning = false;

    protected override void OnEnable()
    {
      abortType = AbortType.Self;
      base.OnEnable();
    }

    public override void OnEnter()
    {
      _counter = 0f;
      _bChildRunning = false;
      base.OnEnter();
    }

    public override bool Condition()
    {
      if (_counter >= timeLimit)
      {
        return false;
      }

      return true;
    }

    /// <summary>
    /// Constatly ticks, so we can update the timer here only if the child is running.
    /// </summary>
    /// <returns></returns>
    protected override bool Reevaluate()
    {
      if (_bChildRunning)
      {

        _counter += Time.deltaTime;
        return base.Reevaluate();
      }

      return false;
    }

    protected internal override void OnChildEnter(int childIndex)
    {
      _bChildRunning = true;
    }

    protected internal override void OnChildExit(int childIndex, BehaviourNode.Status childStatus)
    {
      _bChildRunning = false;
    }
  }
}