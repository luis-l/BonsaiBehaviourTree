
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all task nodes.
  /// </summary>
  public abstract class Task : BehaviourNode
  {
    // Does nothing since tasks do not have children.
    public sealed override void OnAbort(ConditionalAbort aborter) { }
    public sealed override void OnChildEnter(int childIndex) { }
    public sealed override void OnChildExit(int childIndex, Status childStatus) { }
    internal sealed override void AddChildInternal(BehaviourNode child) { }
    internal sealed override void RemoveChildrenInternal() { }

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

    // Tasks cannot concurrently execute on branch or tree update.
    public sealed override void OnBranchTick() { }
    public sealed override bool CanTickOnBranch() { return false; }
    public sealed override void OnTreeTick() { }
    public sealed override bool CanTickOnTree() { return false; }

  }
}