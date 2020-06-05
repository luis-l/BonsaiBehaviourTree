
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Locks the tree execution for a certain amount of time.
  /// </summary>
  [NodeEditorProperties("Conditional/", "Condition")]
  public class Cooldown : ConditionalAbort
  {
    [Tooltip("The amount of time to wait at the cooldown decorator.")]
    public float cooldownTime = 1f;

    private float _counter = 0f;
    private bool _bCooldownActive = false;
    private Status _childStatus;

    public override void OnEnter()
    {
      // Whenever we enter the cooldown decorator we always reset the counter.
      _counter = 0f;

      // We can only traverse the child if the cooldown is inactive.
      if (!_bCooldownActive)
      {
        _iterator.Traverse(_child);
      }
    }

    // Abort if the cooldown status changed.
    public override bool Condition()
    {
      return cooldownFinished();
    }

    public override Status Run()
    {
      // If the cooldown is active, then tick the cooldown timer and wait.
      if (_bCooldownActive)
      {

        _counter += Time.deltaTime;

        // The cooldown finished, deactivate and traverse the child.
        if (cooldownFinished())
        {
          _counter = 0f;
          _bCooldownActive = false;
          _iterator.Traverse(_child);
        }
      }

      // The cooldown is not active, which means we can return if the child finished executing.
      else
      {
        // Activate the cool down for the next run if the child succeeded.
        if (_childStatus == Status.Success)
        {
          _bCooldownActive = true;
          return _childStatus;
        }

        // Since the child failed, we do not activate the cooldown.
        else if (_childStatus == Status.Failure)
        {
          _bCooldownActive = false;
          return _childStatus;
        }
      }

      // Either running child or waiting for the cooldown timer.
      return Status.Running;
    }

    // This only ticks if the current running node is not itself so
    // we do not have to worry about the cooldown being ticked twice (during Run() and Reevaluate())
    protected override bool Reevaluate()
    {
      // If we can re-evaluate then keep ticking so we can abort
      // when the cooldown is over.
      _counter += Time.deltaTime;

      // Make sure to deactivate the cooldown or else the cooldown
      // will run again.
      if (cooldownFinished())
      {

        // We do not want to reset the counter here to 0 since we want
        // the cooldownFinished() to yield False inside of Condition()
        // so the abort fires properly.
        // This is ok to do because OnEnter() resets the counter to 0.

        _bCooldownActive = false;
      }

      return base.Reevaluate();
    }

    protected internal override void OnChildExit(int childIndex, Status childStatus)
    {
      _childStatus = childStatus;
    }

    private bool cooldownFinished()
    {
      return _counter >= cooldownTime;
    }
  }
}