
using System.Text;

namespace Bonsai.Core
{
  public enum AbortType { None, LowerPriority, Self, Both };

  /// <summary>
  /// A special type of decorator node that has a condition to fire an abort.
  /// </summary>
  public abstract class ConditionalAbort : Decorator
  {
    /// <summary>
    /// A property for decorator nodes that allows the flow
    /// of the behaviour tree to change to this node if the
    /// condition is satisfied.
    /// </summary>
    public AbortType abortType = AbortType.None;

    public bool IsObserving { get; private set; } = false;
    private bool IsActive { get; set; } = false;

    /// <summary>
    /// The condition that needs to be satisfied for the node to run its children or to abort.
    /// </summary>
    /// <returns></returns>
    public abstract bool Condition();

    /// <summary>
    /// Called when the observer starts.
    /// This can be used to subscribe to events.
    /// </summary>
    protected virtual void OnObserverBegin() { }

    /// <summary>
    /// Called when the observerer stops.
    /// This can be used unsubscribe from events.
    /// </summary>
    protected virtual void OnObserverEnd() { }

    /// <summary>
    /// Only runs the child if the condition is true.
    /// </summary>
    public override void OnEnter()
    {
      IsActive = true;

      // Observer has become relevant in current context.
      if (abortType != AbortType.None)
      {
        if (!IsObserving)
        {
          IsObserving = true;
          OnObserverBegin();
        }
      }

      if (Condition())
      {
        base.OnEnter();
      }
    }

    public override void OnExit()
    {
      // Observer no longer relevant in current context.
      if (abortType == AbortType.None || abortType == AbortType.Self)
      {
        if (IsObserving)
        {
          IsObserving = false;
          OnObserverEnd();
        }
      }

      IsActive = false;
    }

    // When the parent composite exits, all observers in child branches become irrelevant.
    public sealed override void OnCompositeParentExit()
    {
      if (IsObserving)
      {
        IsObserving = false;
        OnObserverEnd();
      }

      // Propogate composite parent exit through decorator chain only.
      base.OnCompositeParentExit();
    }

    public override Status Run()
    {
      // Return what the child returns if it ran, else fail.
      return Iterator.LastChildExitStatus.GetValueOrDefault(Status.Failure);
    }

    protected void Evaluate()
    {
      bool conditionResult = Condition();

      if (IsActive && !conditionResult)
      {
        AbortCurrentBranch();
      }

      else if (!IsActive && conditionResult)
      {
        AbortLowerPriorityBranch();
      }
    }

    private void AbortCurrentBranch()
    {
      if (abortType == AbortType.Self || abortType == AbortType.Both)
      {
        Iterator.AbortRunningChildBranch(Parent, ChildOrder);
      }
    }

    private void AbortLowerPriorityBranch()
    {
      if (abortType == AbortType.LowerPriority || abortType == AbortType.Both)
      {
        GetCompositeParent(this, out BehaviourNode compositeParent, out int branchIndex);

        if (compositeParent && compositeParent.IsComposite())
        {
          bool isLowerPriority = (compositeParent as Composite).CurrentChildIndex > branchIndex;
          if (isLowerPriority)
          {
            Iterator.AbortRunningChildBranch(compositeParent, branchIndex);
          }
        }
      }
    }

    private static void GetCompositeParent(
      BehaviourNode child,
      out BehaviourNode compositeParent,
      out int branchIndex)
    {
      compositeParent = child.Parent;
      branchIndex = child.indexOrder;

      while (compositeParent && !compositeParent.IsComposite())
      {
        branchIndex = compositeParent.indexOrder;
        compositeParent = compositeParent.Parent;
      }
    }

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("Aborts {0}", abortType.ToString());
    }
  }
}