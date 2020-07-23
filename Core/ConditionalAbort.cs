
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

    private bool lastConditionResult = false;

    /// <summary>
    /// The condition that needs to be satisfied for the node to run its children or to abort.
    /// </summary>
    /// <returns></returns>
    public abstract bool Condition();

    /// <summary>
    /// Only runs the child if the condition is true.
    /// </summary>
    public override void OnEnter()
    {
      lastConditionResult = Condition();
      if (lastConditionResult)
      {
        base.OnEnter();
      }
    }

    /// <summary>
    /// Returns true if the condition changed state since the last re-evaluation.
    /// </summary>
    /// <returns></returns>
    public bool Reevaluate()
    {
      lastConditionResult = Condition();
      return lastConditionResult;
    }

    public override Status Run()
    {
      // Return failure if the condition failed, else
      // return what the child returns if the condition was true.
      return lastConditionResult ? Iterator.LastStatusReturned : Status.Failure;
    }

    public bool IsAbortSatisfied()
    {
      // Aborts need to be enabled in order to test them.
      if (abortType == AbortType.None)
      {
        return false;
      }

      // The main node we wish to abort from if possible.
      // Aborts only occur within the same parent subtree of the aborter.
      BehaviourNode active = Tree.AllNodes[Iterator.CurrentIndex];

      // The abort type dictates the final criteria to check
      // if the abort is satisfied and if the condition check changed state.
      switch (abortType)
      {
        case AbortType.LowerPriority:
          return
              !BehaviourTree.IsUnderSubtree(this, active) &&
              BehaviourTree.IsUnderSubtree(Parent, active) &&
              Priority() > Iterator.GetRunningSubtree(Parent).Priority() &&
              Reevaluate();

        // Self aborts always interrupt, regardless of the condition.
        case AbortType.Self:
          return BehaviourTree.IsUnderSubtree(this, active) && Reevaluate();

        case AbortType.Both:
          return
               (BehaviourTree.IsUnderSubtree(this, active) ||
               (BehaviourTree.IsUnderSubtree(Parent, active) &&
               Priority() > Iterator.GetRunningSubtree(Parent).Priority())) &&
               Reevaluate();
      }

      return false;
    }

    /// <summary>
    /// Test if the aborter may abort the node.
    /// Make sure that the node orders are pre-computed before calling this function.
    /// This method is mainly used by the editor.
    /// </summary>
    /// <param name="aborter">The node to perform the abort.</param>
    /// <param name="node">The node that gets aborted.</param>
    /// <returns></returns>
    public static bool IsAbortable(ConditionalAbort aborter, BehaviourNode node)
    {
      // This makes sure that dangling nodes do not show that they can abort nodes under main tree.
      if (aborter.preOrderIndex == kInvalidOrder)
      {
        return false;
      }

      // Parallel subtrees cannot abort each other.
      if (aborter.Parent && aborter.Parent is ParallelComposite)
      {
        return false;
      }

      switch (aborter.abortType)
      {
        case AbortType.LowerPriority:
          return
              !BehaviourTree.IsUnderSubtree(aborter, node) &&
              BehaviourTree.IsUnderSubtree(aborter.Parent, node) &&
              aborter.Priority() > GetSubtree(aborter.Parent, node).Priority();

        // Self aborts always interrupt, regardless of the condition.
        case AbortType.Self:
          return BehaviourTree.IsUnderSubtree(aborter, node);

        case AbortType.Both:
          return
               BehaviourTree.IsUnderSubtree(aborter, node) ||
               (BehaviourTree.IsUnderSubtree(aborter.Parent, node) &&
               aborter.Priority() > GetSubtree(aborter.Parent, node).Priority());
      }

      return false;
    }

    private static BehaviourNode GetSubtree(BehaviourNode parent, BehaviourNode grandchild)
    {
      BehaviourNode sub = grandchild;
      while (sub.Parent != parent)
      {
        sub = sub.Parent;
      }
      return sub;
    }

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("Aborts {0}", abortType.ToString());
    }
  }
}