
using System;
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
        _iterator.Traverse(next);
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

    public void AddChildren(params BehaviourNode[] children)
    {
      foreach (var child in children)
      {
        AddChild(child);
      }
    }

    /// <summary>
    /// Adds a child if it is parentless.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void AddChild(BehaviourNode child)
    {
      if (child == null)
      {
        Debug.LogWarning("Child is null");
        return;
      }

      if (child == this)
      {
        Debug.LogWarning("A child cannot be its own child.");
        return;
      }

      if (child.Parent == null)
      {
        child.Parent = this;
        child._indexOrder = _children.Count;
        _children.Add(child);
      }

      else
      {
        Debug.LogWarning("Composite node attempted to parent a child that already has a set parent.");
      }
    }

    public sealed override bool CanAddChild(BehaviourNode child)
    {
      return child != null && child != this && child.Parent == null;
    }

    /// <summary>
    /// Removes the child from its children, if it is the parent of the child.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void RemoveChild(BehaviourNode child)
    {
      if (child == null)
      {
        return;
      }

      // Assure that this child was actually parented to this composite node.
      if (child.Parent == this)
      {
        // Forget about this child.
        bool bRemoved = _children.Remove(child);

        // If removed then we unparent the child.
        if (bRemoved)
        {
          child._indexOrder = 0;
          child._parent = null;

          UpdateIndexOrders();
        }

        // BIG ERROR. This should not happen.
        // The theory is that the only way for a child to have its parent set if it was null
        // which gets handled internally by the standard node types: Composite and Decorator.
        else
        {
          const string msg1 = "Error on CompositeNode.Remove(child). ";
          const string msg2 = "A child was parented to a composite node but was not found in the children list. ";
          const string msg3 = "This should not have happend.";

          Debug.LogError(string.Concat(msg1, msg2, msg3));
        }
      }
    }

    public sealed override void ClearChildren()
    {
      // Hack to run some code when removing children.
      Predicate<BehaviourNode> match = (child) =>
      {
        child._indexOrder = 0;
        child._parent = null;
        return true;
      };

      _children.RemoveAll(match);
    }

    /// <summary>
    /// DANGER! 
    /// Directly sets the child (at its relative index.
    /// This is used to help clone nodes.
    /// </summary>
    /// <param name="child"></param>
    public sealed override void ForceSetChild(BehaviourNode child)
    {
      child.ClearParent();
      child.Parent = this;

      // Do not bother with unsetting the original child's parent.
      // The original child is already properly setup in its tree.
      // This is used when trying to build a tree copy, so we can
      // simply set a new child at that index.
      _children[child.ChildOrder] = child;
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
        child._indexOrder = indexOrder++;
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
        _currentChildIndex = child._indexOrder;
      }

      else
      {
        Debug.LogError("The node is not parented to this composite node.");
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