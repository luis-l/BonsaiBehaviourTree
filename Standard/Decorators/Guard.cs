
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bonsai.Core;
using UnityEngine;

namespace Bonsai.Standard
{
  [BonsaiNode("Decorators/", "Shield")]
  public class Guard : Decorator
  {
    public int maxActiveGuards = 1;

    [Tooltip(
      @"If true, then the guard will stay running until the child
      can be used (active guard count < max active guards), else
      the guard will immediately return.")]
    public bool waitUntilChildAvailable = false;

    [Tooltip("When the guard does not wait, should we return success of failure when skipping it?")]
    public bool returnSuccessOnSkip = false;

    [HideInInspector]
    public List<Guard> linkedGuards = new List<Guard>();

    private int runningGuards = 0;
    private bool childRan = false;

    public override void OnEnter()
    {
      // Do not run the child immediately.
    }

    public override Status Run()
    {
      // If we enter the run state of the guard, that means
      // the child already returned.
      if (childRan)
      {
        return Iterator.LastChildExitStatus.GetValueOrDefault(Status.Failure);
      }

      bool bGuardsAvailable = IsRunningGuardsAvailable();

      // Cannot wait for the child, return.
      if (!waitUntilChildAvailable && !bGuardsAvailable)
      {
        return returnSuccessOnSkip ? Status.Success : Status.Failure;
      }

      else if (!childRan && bGuardsAvailable)
      {
        // Notify the other guards that this guard runned its child.
        for (int i = 0; i < linkedGuards.Count; ++i)
        {
          linkedGuards[i].runningGuards += 1;
        }

        childRan = true;
        Iterator.Traverse(Child);
      }

      // Wait for child.
      return Status.Running;
    }

    // Makes sure that the running guards does not exceed that max capacity.
    private bool IsRunningGuardsAvailable()
    {
      return runningGuards < maxActiveGuards;
    }

    public override void OnExit()
    {
      if (childRan)
      {
        runningGuards -= 1;

        // Notify the rest of the guards that this guard finished.
        for (int i = 0; i < linkedGuards.Count; ++i)
        {
          linkedGuards[i].runningGuards -= 1;
        }
      }

      childRan = false;
    }

    public override void OnCopy()
    {
      // Only get the instance version of guards under the tree root.
      linkedGuards = linkedGuards
        .Where(i => i.PreOrderIndex != kInvalidOrder)
        .Select(i => BehaviourTree.GetInstanceVersion<Guard>(Tree, i))
        .ToList();
    }

    public override BehaviourNode[] GetReferencedNodes()
    {
      return linkedGuards.ToArray();
    }

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("Guarding {0}", linkedGuards.Count);
      builder.AppendLine();
      builder.AppendFormat("Active allowed: {0}", maxActiveGuards);
      builder.AppendLine();
      builder.AppendLine(waitUntilChildAvailable ? "Wait for child branch" : "Skip child branch");
      builder.Append(returnSuccessOnSkip ? "Succeed on skip" : "Fail on skip");
    }
  }
}