using System.Collections.Generic;
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all composite nodes.
  /// </summary>
  public abstract class Composite : BehaviourNode
  {
    [SerializeField, HideInInspector]
    protected List<BehaviourNode> _children = new List<BehaviourNode>();

    protected Status lastChildExitStatus;
    protected int CurrentChildIndex { get; private set; } = 0;

    /// <summary>
    /// Default behaviour is sequential from left to right.
    /// </summary>
    /// <returns></returns>
    public virtual BehaviourNode NextChild()
    {
      if (IsInChildrenBounds(CurrentChildIndex))
      {
        return _children[CurrentChildIndex];
      }

      return null;
    }

    /// <summary>
    /// Default behaviour is to immediately traverse the first child.
    /// </summary>
    public override void OnEnter()
    {
      CurrentChildIndex = 0;
      var next = NextChild();
      if (next)
      {
        Iterator.Traverse(next);
      }
    }

    public IEnumerable<BehaviourNode> Children
    {
      get { return _children; }
    }

    public sealed override int ChildCount()
    {
      return _children.Count;
    }

    public sealed override BehaviourNode GetChildAt(int index)
    {
      return _children[index];
    }

    internal sealed override void AddChildInternal(BehaviourNode child)
    {
      if (child != null && child != this)
      {
        _children.Add(child);
      }
    }

    internal sealed override void RemoveChildrenInternal()
    {
      _children.Clear();
    }

    public bool IsInChildrenBounds(int index)
    {
      return index >= 0 && index < _children.Count;
    }

    /// <summary>
    /// Called when a composite node has a child that activates when it aborts.
    /// </summary>
    /// <param name="child"></param>
    public override void OnAbort(ConditionalAbort child)
    {
      // The default behaviour is to set the current child index of the composite
      // node to this child's index.
      if (child.Parent == this)
      {
        CurrentChildIndex = child.indexOrder;
      }
    }

    public override void OnChildExit(int childIndex, Status childStatus)
    {
      CurrentChildIndex++;
      lastChildExitStatus = childStatus;
    }

    public sealed override int MaxChildCount()
    {
      return int.MaxValue;
    }

  }
}