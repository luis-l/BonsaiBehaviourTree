
using System.Text;
using Bonsai.Core;

namespace Bonsai.Standard
{
  /// <summary>
  /// Locks the tree execution for a certain amount of time.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
  public class Cooldown : ConditionalAbort
  {
    [ShowAtRuntime]
    [UnityEngine.SerializeField]
    public Utility.Timer timer = new Utility.Timer();

    public override void OnStart()
    {
      // When the timer finishes, automatically unregister from tree update.
      timer.OnTimeout += RemoveTimerFromTreeTick;
    }

    public override void OnEnter()
    {
      // We can only traverse the child if the cooldown is inactive.
      if (timer.IsDone)
      {
        Iterator.Traverse(child);
      }
    }

    public override void OnExit()
    {
      // Only start time if not yet running 
      if (timer.IsDone)
      {
        Tree.AddTimer(timer);
        timer.Start();
      }
    }

    // Abort if the cooldown status changed.
    public override bool Condition()
    {
      return timer.IsDone;
    }

    public override Status Run()
    {
      // If the cooldown is active, fail to lock the branch from running.
      if (timer.IsRunning)
      {
        return Status.Failure;
      }

      // Cooldown is not active, pass the child branch status.
      return Iterator.LastChildExitStatus.GetValueOrDefault(Status.Failure);
    }

    private void RemoveTimerFromTreeTick()
    {
      Tree.RemoveTimer(timer);

      // Time is done. Notify abort.
      Evaluate();
    }

    public override void Description(StringBuilder builder)
    {
      base.Description(builder);
      builder.AppendLine();
      builder.AppendFormat("Lock execution for {0:0.00}s", timer.interval);
    }
  }
}