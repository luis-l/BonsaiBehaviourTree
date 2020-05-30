
using System.Collections.Generic;
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Decorators/", "Shield")]
  public class Guard : Decorator
  {
    public int maxActiveGuards = 1;

    [Tooltip("If true, then the guard will stay running until the child" +
        "can be used (active guard count < max active guards), else " +
        "the guard will immediately return.")]
    public bool waitUntilChildAvailable = false;

    [Tooltip("When the guard does not wait, should we return success of failure when skipping it?")]
    public bool returnSuccessOnSkip = false;

    [Tooltip("Linked guards to test against.")]
    public List<Guard> linkedGuards = new List<Guard>();

    private int _runningGuards = 0;
    private bool _bChildRan = false;

    public override void OnEnter()
    {
      // Do not run the child immediately.
    }

    public override Status Run()
    {
      // If we enter the run state of the guard, that means
      // the child already returned.
      if (_bChildRan)
      {
        return _iterator.LastStatusReturned;
      }

      bool bGuardsAvailable = isRunningGuardsAvailable();

      // Cannot wait for the child, return.
      if (!waitUntilChildAvailable && !bGuardsAvailable)
      {
        return returnSuccessOnSkip ? Status.Success : Status.Failure;
      }

      else if (!_bChildRan && bGuardsAvailable)
      {

        // Notify the other guards that this guard runned its child.
        for (int i = 0; i < linkedGuards.Count; ++i)
        {
          linkedGuards[i]._runningGuards += 1;
        }

        _bChildRan = true;
        _iterator.Traverse(Child);
      }

      // Wait for child.
      return Status.Running;
    }

    // Makes sure that the running guards does not exceed that max capacity.
    private bool isRunningGuardsAvailable()
    {
      return _runningGuards < maxActiveGuards;
    }

    public override void OnExit()
    {
      if (_bChildRan)
      {

        _runningGuards -= 1;

        // Notify the rest of the guards that this guard finished.
        for (int i = 0; i < linkedGuards.Count; ++i)
        {
          linkedGuards[i]._runningGuards -= 1;
        }
      }

      _bChildRan = false;
    }

    public override void OnCopy()
    {
      for (int i = 0; i < linkedGuards.Count; ++i)
      {
        linkedGuards[i] = BehaviourTree.GetInstanceVersion<Guard>(Tree, linkedGuards[i]);
      }
    }

    public override BehaviourNode[] GetReferencedNodes()
    {
      return linkedGuards.ToArray();
    }
  }
}