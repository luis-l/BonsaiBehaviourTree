
using Bonsai.Core;

namespace Bonsai.Standard
{
  [BonsaiNode("Decorators/", "Interruptable")]
  public class Interruptable : Decorator
  {
    private bool isRunning = false;
    private Status returnStatus = Status.Failure;
    private bool isInterrupted = false;

    public override void OnEnter()
    {
      isRunning = true;
      isInterrupted = false;
      base.OnEnter();
    }

    public override Status Run()
    {
      if (isInterrupted)
      {
        return returnStatus;
      }

      return Iterator.LastChildExitStatus.GetValueOrDefault(Status.Failure);
    }

    public override void OnExit()
    {
      isRunning = false;
    }

    public void PerformInterruption(Status interruptionStatus)
    {
      if (isRunning)
      {
        isInterrupted = true;
        returnStatus = interruptionStatus;
        BehaviourTree.Interrupt(Child);
      }
    }
  }
}