
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
    /// Default behaviour is to immediately try to traverse its child.
    /// </summary>
    public override void OnEnter()
    {
      _iterator.Traverse(_child);
    }

    /// <summary>
    /// Sets the child.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void AddChild(BehaviourNode child)
    {
      // Cannot be null and must be different.
      if (child == null || child == _child)
      {
        return;
      }

      if (child.Parent == null)
      {

        // Unparent the current child from this decorator.
        RemoveChild(_child);

        // Assign the new child and parent it to this decorator.
        _child = child;
        _child.Parent = this;
      }

      else
      {
        Debug.LogWarning("Cannot set child since it already has a parent.");
      }
    }

    /// <summary>
    /// Gets the child.
    /// </summary>
    public BehaviourNode Child
    {
      get { return _child; }
    }

    /// <summary>
    /// Removes the child, if it is the parent of the child.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void RemoveChild(BehaviourNode child)
    {
      // Cannot be null and child must match.
      if (_child == null || _child != child)
      {
        return;
      }

      // Unparent the child.
      _child._parent = null;

      // This decorator forgets about its child.
      _child = null;
    }

    /// <summary>
    /// Removes the child.
    /// </summary>
    public sealed override void ClearChildren()
    {
      RemoveChild(_child);
    }

    /// <summary>
    /// DANGER! 
    /// Directly sets the child.
    /// This is used to help clone nodes.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void ForceSetChild(BehaviourNode child)
    {
      child.ClearParent();

      // Do not bother with unsetting the original child's parent.
      // The original child is already properly setup in its tree.
      // This is used when trying to build a tree copy, so we can
      // simply null the reference and set a new child.
      _child = null;

      AddChild(child);
    }

    protected internal sealed override void OnAbort(ConditionalAbort aborter) { }

    public sealed override bool CanAddChild(BehaviourNode child)
    {
      return child != null && child.Parent == null && child != this;
    }

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