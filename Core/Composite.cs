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

    protected Status _previousChildExitStatus;
    protected int _currentChildIndex = 0;

    /// <summary>
    /// Default behaviour is sequential from left to right.
    /// </summary>
    /// <returns></returns>
    public virtual BehaviourNode NextChild()
    {
      if (IsInChildrenBounds(_currentChildIndex))
      {
        return _children[_currentChildIndex];
      }

      return null;
    }

    /// <summary>
    /// Default behaviour is to immediately traverse the first child.
    /// </summary>
    public override void OnEnter()
    {
      _currentChildIndex = 0;
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

    public BehaviourNode First
    {
      get { return GetChildAt(0); }
    }

    public BehaviourNode Last
    {
      get { return GetChildAt(_children.Count - 1); }
    }

    public void UpdateIndexOrders()
    {
      // Fix other child orders
      int indexOrder = 0;
      foreach (var child in _children)
      {
        child.indexOrder = indexOrder++;
      }
    }

    /// <summary>
    /// Called when a composite node has a child that activates when it aborts.
    /// </summary>
    /// <param name="child"></param>
    public override void OnAbort(ConditionalAbort child)
    {
      // The default behaviour is to set the current child index of the composite
      // node to this child's index.
      if (IsChild(child))
      {
        _currentChildIndex = child.indexOrder;
      }
    }

    public override void OnChildExit(int childIndex, Status childStatus)
    {
      _currentChildIndex++;
      _previousChildExitStatus = childStatus;
    }

    public sealed override int MaxChildCount()
    {
      return int.MaxValue;
    }

    /// <summary>
    /// Tests if a node is a child of this composite node.
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public bool IsChild(BehaviourNode child)
    {
      return child.Parent != null && child.Parent.preOrderIndex == preOrderIndex;
    }

#if UNITY_EDITOR
    /// <summary>
    /// DANGER, this does not properly handle unparenting.
    /// This is used for positional reordering in the editor.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="index"></param>
    public void SetChildAtIndex(BehaviourNode node, int index)
    {
      _children[index] = node;
    }
#endif

  }
}