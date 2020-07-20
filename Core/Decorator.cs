
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all decorators.
  /// </summary>
  public abstract class Decorator : BehaviourNode
  {
    [SerializeField, HideInInspector]
    protected BehaviourNode _child;

    /// <summary>
    /// Gets the child.
    /// </summary>
    public BehaviourNode Child
    {
      get { return _child; }
    }

    /// <summary>
    /// Default behaviour is to immediately try to traverse its child.
    /// </summary>
    public override void OnEnter()
    {
      if (_child)
      {
        _iterator.Traverse(_child);
      }
    }

    /// <summary>
    /// Sets the child.
    /// </summary>
    /// <param name="child"></param>
    internal sealed override void AddChildInternal(BehaviourNode child)
    {
      _child = child;
    }

    /// <summary>
    /// Unsets the child.
    /// </summary>
    /// 
    internal sealed override void RemoveChildrenInternal()
    {
      // This decorator forgets about its child.
      _child = null;
    }

    public sealed override void OnAbort(ConditionalAbort aborter) { }

    public sealed override int MaxChildCount()
    {
      return 1;
    }

    public sealed override int ChildCount()
    {
      return _child == null ? 0 : 1;
    }

    public sealed override BehaviourNode GetChildAt(int index)
    {
      return _child;
    }
  }
}