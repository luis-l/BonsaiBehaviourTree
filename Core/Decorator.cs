
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all decorators.
  /// </summary>
  public abstract class Decorator : BehaviourNode
  {
    [SerializeField, HideInInspector]
    protected BehaviourNode child;

    /// <summary>
    /// Gets the child.
    /// </summary>
    public BehaviourNode Child
    {
      get { return child; }
    }

    /// <summary>
    /// Default behaviour is to immediately try to traverse its child.
    /// </summary>
    public override void OnEnter()
    {
      if (child)
      {
        Iterator.Traverse(child);
      }
    }

    /// <summary>
    /// <para>Set the child for the decorator node.</para>
    /// <para>
    /// This should be called <b>once</b> when the tree is being built,
    /// before Tree Start() and never during Tree Update()
    /// </para>
    /// </summary>
    public void SetChild(BehaviourNode node)
    {
      child = node;
      if (child != null)
      {
        child.Parent = this;
        child.indexOrder = 0;
      }
    }

    public sealed override void OnAbort(ConditionalAbort aborter) { }

    public sealed override int MaxChildCount()
    {
      return 1;
    }

    public sealed override int ChildCount()
    {
      return child == null ? 0 : 1;
    }

    public sealed override BehaviourNode GetChildAt(int index)
    {
      return child;
    }
  }
}