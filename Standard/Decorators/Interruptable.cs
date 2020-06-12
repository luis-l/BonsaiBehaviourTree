
using Bonsai.Designer;
using Bonsai.Core;

namespace Bonsai.Standard
{
  [BonsaiNode("Decorators/", "Interruptable")]
  public class Interruptable : Decorator
  {
    public bool _bIsRunning = false;

    private Status _statusToReturn = Status.Failure;
    private bool _bInterrupted = false;

    public override void OnEnter()
    {
      _bIsRunning = true;
      _bInterrupted = false;
      base.OnEnter();
    }

    public override Status Run()
    {
      if (_bInterrupted)
      {
        return _statusToReturn;
      }

      return _iterator.LastStatusReturned;
    }

    public override void OnExit()
    {
      _bIsRunning = false;
    }

    public void PerformInterruption(Status interruptionStatus)
    {
      if (_bIsRunning)
      {

        _bInterrupted = true;
        _statusToReturn = interruptionStatus;
        Tree.Interrupt(this);
      }
    }
  }
}