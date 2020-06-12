
using System.Collections.Generic;
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [BonsaiNode("Tasks/", "Interruptor")]
  public class Interruptor : Task
  {
    [Tooltip("If true, then the interruptable node return success else failure.")]
    public bool returnSuccess = false;
    public List<Interruptable> linkedInterruptables = new List<Interruptable>();

    public override Status Run()
    {
      for (int i = 0; i < linkedInterruptables.Count; ++i)
      {

        Status interruptionStatus = returnSuccess ? Status.Success : Status.Failure;

        linkedInterruptables[i].PerformInterruption(interruptionStatus);
      }

      return Status.Success;
    }

    public override void OnCopy()
    {
      for (int i = 0; i < linkedInterruptables.Count; ++i)
      {
        linkedInterruptables[i] = BehaviourTree.GetInstanceVersion<Interruptable>(Tree, linkedInterruptables[i]);
      }
    }

    public override BehaviourNode[] GetReferencedNodes()
    {
      return linkedInterruptables.ToArray();
    }
  }
}