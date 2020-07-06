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

    [SerializeField, HideInInspector]
    private BehaviourTree _parentTree = null;

    [SerializeField, HideInInspector]
    internal int preOrderIndex = 0;

    [SerializeField, HideInInspector]
    internal int postOrderIndex = 0;

    [SerializeField, HideInInspector]
    internal int levelOrder = 0;

    [SerializeField, HideInInspector]
    protected internal BehaviourNode _parent;

    internal BehaviourIterator _iterator;

    /// <summary>
    /// The order of the node relative to its parent.
    /// This value is only changed in the Composite node, since
    /// that can affect the index value in the AddChild() and RemoveChild().
    ///
    /// This value is mainly used when the iterator needs to jump to a child
    /// of a composite node.
    ///
    /// It is also used in tree cloning.
    /// </summary>
    [SerializeField, HideInInspector]
    protected internal int _indexOrder = 0;

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
      get { return _parentTree; }

      set
      {
        if (!_parentTree)
        {

          _parentTree = value;
          _parentTree.allNodes.Add(this);
        }

        else
        {
          Debug.LogError("The tree can only be set once.");
        }
      }
    }

    /// <summary>
    /// The index position of the node under its parent (if any).
    /// </summary>
    public int ChildOrder
    {
      get { return _indexOrder; }
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
    /// Gets and Sets the Node's parent.
    /// </summary>
    public BehaviourNode Parent
    {
      get { return _parent; }
      set
      {
        // If there is no parent associated.
        if (_parent == null)
        {

          // Assign the parent.
          _parent = value;
        }

        // Error. Need to remove the old parent first, before setting a new one.
        else
        {
          Debug.LogWarning("Behaviour Node already has a parent!");
          Debug.Log("Remove the current parent first if you wish to set a new one.");
        }
      }
    }

    public BehaviourIterator Iterator
    {
      get { return _iterator; }
    }

    /// <summary>
    /// Gets the blackboard used by the parent tree.
    /// </summary>
    protected Blackboard Blackboard
    {
      get { return _parentTree.Blackboard; }
    }

    /// <summary>
    /// The game object associated with the tree of this node.
    /// </summary>
    protected GameObject Actor
    {
      get { return _parentTree.actor; }
    }

    /// <summary>
    /// DANGER. Directly sets the tree reference to null.
    /// </summary>
    internal void ClearTree()
    {
      _parentTree = null;
    }

    public virtual void OnDrawGizmos() { }

    public abstract BehaviourNode GetChildAt(int index);
    public abstract int ChildCount();

    /// <summary>
    /// DANGER.
    /// A method that removes the reference to the parent.
    /// NOTE: This method is only used to help clone nodes.
    /// There is no need for you to use it!.
    /// </summary>
    internal void ClearParent()
    {
      _parent = null;
    }

    /// <summary>
    /// DANGER! 
    /// Directly sets the child (at its relative index.
    /// This is used to help clone nodes.
    /// </summary>
    /// <param name="child"></param>
    public abstract void ForceSetChild(BehaviourNode child);

    // These are functions used by the Tree cloner and the editor.
    public abstract void AddChild(BehaviourNode child);
    public abstract void RemoveChild(BehaviourNode child);
    public abstract void ClearChildren();
    public abstract bool CanAddChild(BehaviourNode child);

    public abstract int MaxChildCount();

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