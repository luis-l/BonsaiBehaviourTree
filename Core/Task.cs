
namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all task nodes.
  /// </summary>
  public abstract class Task : BehaviourNode
  {
    // No-Ops since Tasks do not have children. These will never be invoked by the Tree.
    public sealed override void OnAbort(int childIndex) { }
    public sealed override void OnChildEnter(int childIndex) { }
    public sealed override void OnChildExit(int childIndex, Status childStatus) { }

    // Tasks do not have children.
    public sealed override int MaxChildCount() { return 0; }
    public sealed override int ChildCount() { return 0; }
    public sealed override BehaviourNode GetChildAt(int index) { return null; }
  }
}