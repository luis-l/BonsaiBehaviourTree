
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all task nodes.
  /// </summary>
  public abstract class Task : BehaviourNode
  {
    // Does nothing since tasks do not have children.
    protected internal sealed override void OnAbort(ConditionalAbort aborter) { }
    protected internal sealed override void OnChildEnter(int childIndex) { }
    protected internal sealed override void OnChildExit(int childIndex, Status childStatus) { }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void AddChild(BehaviourNode child) { }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void RemoveChild(BehaviourNode child) { }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public sealed override void ClearChildren() { }

    /// <summary>
    /// Always returns false.
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public sealed override bool CanAddChild(BehaviourNode child)
    {
      return false;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void ForceSetChild(BehaviourNode child) { }

    public sealed override int MaxChildCount()
    {
      return 0;
    }

    /// <summary>
    /// Always returns 0.
    /// </summary>
    /// <returns></returns>
    public sealed override int ChildCount()
    {
      return 0;
    }

    /// <summary>
    /// Always returns null.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public sealed override BehaviourNode GetChildAt(int index)
    {
      return null;
    }

    // Tasks cannot concurrently execute on branch update.
    public sealed override void OnBranchTick() { }
    public sealed override bool CanTickOnBranch() { return false; }

  }
}