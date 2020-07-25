
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all composite nodes.
  /// </summary>
  public abstract class Composite : BehaviourNode
  {
    [SerializeField, HideInInspector]
    private BehaviourNode[] children;

    protected Status lastChildExitStatus;
    public int CurrentChildIndex { get; private set; } = 0;

    public virtual BehaviourNode CurrentChild()
    {
      if (CurrentChildIndex < children.Length)
      {
        return children[CurrentChildIndex];
      }

      return null;
    }

    /// <summary>
    /// Default behaviour is to immediately traverse the first child.
    /// </summary>
    public override void OnEnter()
    {
      CurrentChildIndex = 0;
      var next = CurrentChild();
      if (next)
      {
        Iterator.Traverse(next);
      }
    }

    public BehaviourNode[] Children
    {
      get { return children; }
    }

    public sealed override int ChildCount()
    {
      return children.Length;
    }

    public sealed override BehaviourNode GetChildAt(int index)
    {
      return children[index];
    }

    /// <summary>
    /// <para>Set the children for the composite node.</para>
    /// <para>This should be called when the tree is being built.</para>
    /// <para>It should be called before Tree Start() and never during Tree Update()</para>
    /// <note>To clear children references, pass an empty array.</note>
    /// </summary>
    /// <param name="nodes">The children for the node. Should not be null.</param>
    public void SetChildren(BehaviourNode[] nodes)
    {
      children = nodes;
      // Set index orders.
      for (int i = 0; i < children.Length; i++)
      {
        children[i].indexOrder = i;
      }

      // Set parent references.
      foreach (BehaviourNode child in children)
      {
        child.Parent = this;
      }
    }

    /// <summary>
    /// Called when a composite node has a child that activates when it aborts.
    /// </summary>
    /// <param name="child"></param>
    public override void OnAbort(int childIndex)
    {
      // The default behaviour is to set the current child index of the composite node.
      CurrentChildIndex = childIndex;
    }

    /// <summary>
    /// Default behaviour is sequential traversal from first to last.
    /// </summary>
    /// <returns></returns>
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