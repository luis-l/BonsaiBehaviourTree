using System.Text;
using UnityEngine;

namespace Bonsai.Core
{
  /// <summary>
  /// The base class for all behaviour nodes.
  /// </summary>
  public abstract class BehaviourNode : ScriptableObject, IIterableNode<BehaviourNode>
  {
    /// <summary>
    /// The return status of a node execution.
    /// </summary>
    public enum Status
    {
      Success, Failure, Running
    };

    public const int kInvalidOrder = -1;

    internal BehaviourTree treeOwner = null;

    [SerializeField, HideInInspector]
    internal int preOrderIndex = 0;

    internal int postOrderIndex = 0;
    internal int levelOrder = 0;

    public BehaviourNode Parent { get; private set; }
    public BehaviourIterator Iterator { get; internal set; }

    /// <summary>
    /// The order of the node relative to its parent.
    /// </summary>
    protected internal int indexOrder = 0;

    protected virtual void OnEnable() { }

    /// <summary>
    /// Called when the tree is started.
    /// </summary>
    public virtual void OnStart() { }

    /// <summary>
    /// Executes when the node is at the top of the execution.
    /// </summary>
    /// <returns></returns>
    public abstract Status Run();

    /// <summary>
    /// Called when a traversal begins on the node.
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Called when a traversal on the node ends.
    /// </summary>
    public virtual void OnExit() { }

    /// <summary>
    /// Executes every tick when the branch is active.
    /// Can be used to run concurrent behaviour.
    /// </summary>
    public virtual void OnBranchTick() { }

    /// <summary>
    /// Default behaviour is to not run OnBranchTick.
    /// </summary>
    public virtual bool CanTickOnBranch() { return false; }


    /// <summary>
    /// Exectues every tick when the tree is active.
    /// Can be used to tick concurrent behaviour in the background. e.g. Cooldown timers.
    /// </summary>
    public virtual void OnTreeTick() { }

    /// <summary>
    /// Default behaviour is to not run OnTreeTick.
    /// </summary>
    public virtual bool CanTickOnTree() { return false; }

    /// <summary>
    /// The priority value of the node.
    /// </summary>
    /// <returns>The negated pre-order index, since lower preorders are executed first.</returns>
    public float Priority()
    {
      return -preOrderIndex;
    }

    /// <summary>
    /// Used to evaluate which branch should execute first with the utility selector.
    /// </summary>
    /// <returns></returns>
    public virtual float UtilityValue()
    {
      return 0f;
    }

    /// <summary>
    /// Called when a child fires an abort.
    /// </summary>
    /// <param name="aborter"></param>
    public virtual void OnAbort(ConditionalAbort aborter) { }

    /// <summary>
    /// Called when the iterator traverses the child.
    /// </summary>
    /// <param name="childIndex"></param>
    public virtual void OnChildEnter(int childIndex) { }

    /// <summary>
    /// Called when the iterator exits the the child.
    /// </summary>
    /// <param name="childIndex"></param>
    /// <param name="childStatus"></param>
    public virtual void OnChildExit(int childIndex, Status childStatus) { }

    /// <summary>
    /// Called when after the entire tree is finished being copied.
    /// Should be used to setup special BehaviourNode references.
    /// </summary>
    public virtual void OnCopy() { }

    /// <summary>
    /// A helper method to return nodes that being referenced.
    /// </summary>
    /// <returns></returns>
    public virtual BehaviourNode[] GetReferencedNodes()
    {
      return null;
    }

    /// <summary>
    /// The tree that owns the node.
    /// </summary>
    public BehaviourTree Tree
    {
      get { return treeOwner; }
    }

    /// <summary>
    /// The index position of the node under its parent (if any).
    /// </summary>
    public int ChildOrder
    {
      get { return indexOrder; }
    }

    /// <summary>
    /// The order of the node in pre order.
    /// </summary>
    public int PreOrderIndex
    {
      get { return preOrderIndex; }
    }

    /// <summary>
    /// The order of the node in post order.
    /// </summary>
    public int PostOrderIndex
    {
      get { return postOrderIndex; }
    }

    public int LevelOrder
    {
      get { return levelOrder; }
    }

    /// <summary>
    /// Gets the blackboard used by the parent tree.
    /// </summary>
    protected Blackboard Blackboard
    {
      get { return treeOwner.Blackboard; }
    }

    /// <summary>
    /// The game object associated with the tree of this node.
    /// </summary>
    protected GameObject Actor
    {
      get { return treeOwner.actor; }
    }


    public abstract BehaviourNode GetChildAt(int index);
    public abstract int ChildCount();
    public abstract int MaxChildCount();

    // The current tree implementation does a "add-only" approach
    // to simplify handling. This is because once a tree is built,
    // it will never change during execution.
    //
    // The general work flow to change children: 
    //   remove all children, add new children.
    //
    // This should be good enough for current use case.
    public void AddChild(BehaviourNode child)
    {
      // If unparented, add node.
      if (!child.Parent)
      {
        AddChildOverride(child);
      }
    }

    /// <summary>
    /// Removes all children and unsets their parent node.
    /// </summary>
    public void RemoveChildren()
    {
      for (int i = 0; i < ChildCount(); i++)
      {
        GetChildAt(i).Parent = null;
      }

      RemoveChildrenInternal();
    }

    // Internal implementation to add a child to a node.
    internal abstract void AddChildInternal(BehaviourNode node);

    // Internal implementation to remove all children references.
    internal abstract void RemoveChildrenInternal();

    /// <summary>
    /// Adds child regardless if it has a parent.
    /// </summary>
    internal void AddChildOverride(BehaviourNode child)
    {
      child.indexOrder = ChildCount();
      AddChildInternal(child);
      child.Parent = this;
    }

    /// <summary>
    /// A summary description of the node.
    /// </summary>
    public virtual void Description(StringBuilder builder)
    {
      // Default adds no description
    }

    #region Node Editor Meta Data

#if UNITY_EDITOR

    /// <summary>
    /// Statuses used by the editor to know how to visually represent the node.
    /// It is the same as the Status enum but has extra enums useful to the editor.
    /// </summary>
    public enum StatusEditor
    {
      Success, Failure, Running, None, Aborted, Interruption
    };


    private StatusEditor _statusEditor = StatusEditor.None;

    /// <summary>
    /// Gets the status of the node for editor purposes.
    /// </summary>
    /// <returns></returns>
    public StatusEditor GetStatusEditor()
    {
      return _statusEditor;
    }

    /// <summary>
    /// Converts the runtime status to an editor status.
    /// </summary>
    /// <param name="s"></param>
    public void SetStatusEditor(Status s)
    {
      _statusEditor = (StatusEditor)(int)s;
    }

    public void SetStatusEditor(StatusEditor s)
    {
      _statusEditor = s;
    }

    [Header("Description")]
    public string title;

    [Multiline]
    public string comment;

    [HideInInspector]
    public Vector2 bonsaiNodePosition;

#endif

    #endregion
  }
}