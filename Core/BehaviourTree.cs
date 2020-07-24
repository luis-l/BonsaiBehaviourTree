using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Core
{
  [CreateAssetMenu(fileName = "BonsaiBT", menuName = "Bonsai/Behaviour Tree")]
  public class BehaviourTree : ScriptableObject
  {
    private BehaviourIterator mainIterator;

    // Only conditional decorator nodes can have observer properties.
    private List<ConditionalAbort> observerAborts;

    // Store references to the parallel nodes;
    private ParallelComposite[] parallelNodes;

    /// <summary>
    /// Nodes that are allowed to update on tree tick.
    /// </summary>
    private BehaviourNode[] treeTickNodes;

    public BehaviourNode Root
    {
      get { return allNodes.Count == 0 ? null : allNodes[0]; }
    }

    private bool isTreeInitialized = false;

    /// <summary>
    /// The game object binded to the tree.
    /// This is assigned at runtime when the tree instance starts.
    /// </summary>
    public GameObject actor;

    [SerializeField, HideInInspector]
    private Blackboard _blackboard;

    public Blackboard Blackboard
    {
      get { return _blackboard; }
    }

    // allNodes must always be kept in pre-order.
    [SerializeField, HideInInspector]
    [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Unity cannot serialize readonly fields")]
    private List<BehaviourNode> allNodes = new List<BehaviourNode>();

    public void SetBlackboard(Blackboard bb)
    {
      _blackboard = bb;
    }

    /// <summary>
    /// <para>Preprocesses and starts the tree.
    /// This can be thought of as the tree initializer.
    /// </para>
    /// Does not begin the tree traversal.
    /// <seealso cref="BeginTraversal"/>
    /// </summary>
    public void Start()
    {
      if (Root == null)
      {
        Debug.LogWarning("Cannot start tree with a null root.");
        return;
      }

      PreProcess();

      foreach (BehaviourNode node in allNodes)
      {
        node.OnStart();
      }

      isTreeInitialized = true;
    }

    public void Update()
    {
      if (isTreeInitialized && mainIterator.IsRunning)
      {

        if (treeTickNodes.Length != 0)
        {
          NodeTreeTick();
        }

        if (observerAborts.Count != 0)
        {
          TickObservers();
        }

        mainIterator.Update();
      }
    }

    /// <summary>
    /// <para>Start traversing the tree from the root.
    /// Can only be done if the tree is not yet running.
    /// </para>
    /// The tree should be initialized before calling this.
    /// <seealso cref="Start"/>
    /// </summary>
    public void BeginTraversal()
    {
      if (isTreeInitialized && !mainIterator.IsRunning)
      {
        mainIterator.Traverse(Root);
      }
    }

    /// <summary>
    /// Processes the tree to calculate certain properties like node priorities,
    /// caching observers, and sync parallel iterators.
    /// The root must be set.
    /// </summary>
    void PreProcess()
    {
      if (Root == null)
      {
        Debug.Log("The tree must have a valid root in order to be pre-processed");
        return;
      }

      SetPostandLevelOrders();

      mainIterator = new BehaviourIterator(this, 0);

      // Setup a new list for the observer nodes.
      observerAborts = new List<ConditionalAbort>();

      parallelNodes = GetNodes<ParallelComposite>().ToArray();

      CacheObservers();
      CacheTreeTickNodes();
      SetRootIteratorReferences();
    }

    private void CacheObservers()
    {
      observerAborts.Clear();
      observerAborts.AddRange(
        GetNodes<ConditionalAbort>()
        .Where(node => node.abortType != AbortType.None));
    }

    private void CacheTreeTickNodes()
    {
      treeTickNodes = allNodes.Where(node => node.CanTickOnTree()).ToArray();
    }

    private void SetRootIteratorReferences()
    {
      // Assign the main iterator to nodes not under any parallel nodes.
      // Children under parallel nodes will have iterators assigned by the parallel parent.
      // Each branch under a parallel node use their own branch iterator.
      TreeIterator<BehaviourNode>.Traverse(
        Root,
        delegate { },
        node =>
        {
          node.Iterator = mainIterator;
          return node is ParallelComposite;
        });
    }

    public void Interrupt(BehaviourNode subroot, bool bFullInterrupt = false)
    {
      // Interrupt this subtree.
      subroot.Iterator.StepBackInterrupt(subroot, bFullInterrupt);

      // Look for parallel nodes under the subroot.
      // Since the parallel count is usually small, we 
      // can just do a linear iteration to interrupt multiple
      // parallel nodes.
      foreach (ParallelComposite p in parallelNodes)
      {
        if (IsUnderSubtree(subroot, p))
        {
          foreach (BehaviourIterator itr in p.BranchIterators)
          {
            // Only interrupt running iterators.
            if (itr.IsRunning)
            {
              // Get the child of the parallel node, and interrupt the child subtree.
              int childIndex = itr.FirstInTraversal;
              BehaviourNode firstNode = allNodes[childIndex];
              itr.StepBackInterrupt(firstNode.Parent, bFullInterrupt);
            }
          }
        }
      }
    }

    /// <summary>
    /// Sets the nodes post and level order numbering.
    /// </summary>
    private void SetPostandLevelOrders()
    {
      int orderCounter = 0;
      TreeIterator<BehaviourNode>.Traverse(
        Root,
        node => node.postOrderIndex = orderCounter++,
        Traversal.PostOrder);

      TreeIterator<BehaviourNode>.Traverse(
        Root,
        (node, itr) =>
        {
          node.levelOrder = itr.CurrentLevel;
          Height = itr.CurrentLevel;
        },
        Traversal.LevelOrder);
    }

    /// <summary>
    /// Gets the nodes of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<T> GetNodes<T>() where T : BehaviourNode
    {
      return allNodes.Select(node => node as T).Where(casted => casted != null);
    }

    // Note on multiple aborts:
    // If there are multiple satisfied aborts, then
    // the tree picks the highest order abort (left most).
    private void TickObservers()
    {
      for (int i = 0; i < observerAborts.Count; ++i)
      {
        ConditionalAbort node = observerAborts[i];

        // The iterator must be running since aborts can only occur under 
        // actively running subtrees.
        if (!node.Iterator.IsRunning)
        {
          continue;
        }

        // If the condition is true then apply an abort.
        if (node.IsAbortSatisfied())
        {
          node.Iterator.OnAbort(node);
        }
      }
    }

    private void NodeTreeTick()
    {
      for (int i = 0; i < treeTickNodes.Length; i++)
      {
        treeTickNodes[i].OnTreeTick();
      }
    }

    /// <summary>
    /// Tests if the order of a is lower than b.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool IsLowerOrder(int orderA, int orderB)
    {
      // 1 is the highest priority.
      // Greater numbers means lower priority.
      return orderA > orderB;
    }

    /// <summary>
    /// Tests if the order of a is higher than b.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool IsHigherOrder(int orderA, int orderB)
    {
      return orderA < orderB;
    }

    /// <summary>
    /// Tests if node is under the root tree.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static bool IsUnderSubtree(BehaviourNode root, BehaviourNode node)
    {
      // Assume that this is the root of the tree root.
      // This would happen when checking IsUnderSubtree(node.parent, other)
      if (root == null)
      {
        return true;
      }

      return root.PostOrderIndex > node.PostOrderIndex && root.PreOrderIndex < node.PreOrderIndex;
    }

    public bool IsRunning()
    {
      return mainIterator != null && mainIterator.IsRunning;
    }

    public BehaviourNode.Status LastStatus()
    {
      return mainIterator.LastStatusReturned;
    }

    public int Height { get; private set; } = 0;

    public void SetNodes(BehaviourNode root)
    {
      allNodes.Clear();
      TreeIterator<BehaviourNode>.Traverse(
        root,
        node => AddNode(node));
    }

    private void AddNode(BehaviourNode node)
    {
      node.preOrderIndex = allNodes.Count;
      node.treeOwner = this;
      allNodes.Add(node);
    }

    /// <summary>
    /// Gets the instantiated copy version of a behaviour node from its original version.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="tree">The instantiated tree.</param>
    /// <param name="original">The node in the original tree.</param>
    /// <returns></returns>
    public static T GetInstanceVersion<T>(BehaviourTree tree, BehaviourNode original) where T : BehaviourNode
    {
      return GetInstanceVersion(tree, original) as T;
    }

    public static BehaviourNode GetInstanceVersion(BehaviourTree tree, BehaviourNode original)
    {
      int index = original.preOrderIndex;
      return tree.allNodes[index];
    }

    /// <summary>
    /// Deep copies the tree.
    /// </summary>
    /// <param name="sourceTree">The source tree to clone.</param>
    /// <returns>The cloned tree.</returns>
    public static BehaviourTree Clone(BehaviourTree sourceTree)
    {
      // The tree clone will be blank to start. We will duplicate blackboard and nodes.
      var cloneBt = CreateInstance<BehaviourTree>();
      cloneBt.name = sourceTree.name;

      if (sourceTree._blackboard)
      {
        cloneBt._blackboard = Instantiate(sourceTree._blackboard);
      }

      // This will add nodes in pre-order to the main node list.
      TreeIterator<BehaviourNode>.Traverse(
        sourceTree.Root,
        node => cloneBt.AddNode(Instantiate(node)));

      // Relink children and parents for the cloned nodes.
      int maxCloneNodeCount = cloneBt.allNodes.Count;
      for (int i = 0; i < maxCloneNodeCount; ++i)
      {
        BehaviourNode nodeSource = sourceTree.allNodes[i];
        BehaviourNode copyNode = GetInstanceVersion(cloneBt, nodeSource);

        if (copyNode.IsComposite())
        {
          var copyComposite = copyNode as Composite;
          copyComposite.SetChildren(
            Enumerable.Range(0, nodeSource.ChildCount())
            .Select(childIndex => GetInstanceVersion(cloneBt, nodeSource.GetChildAt(childIndex)))
            .ToArray());
        }

        else if (copyNode.IsDecorator() && nodeSource.ChildCount() == 1)
        {
          var copyDecorator = copyNode as Decorator;
          copyDecorator.SetChild(GetInstanceVersion(cloneBt, nodeSource.GetChildAt(0))); ;
        }

      }

      foreach (BehaviourNode node in cloneBt.allNodes)
      {
        node.OnCopy();
      }

      return cloneBt;
    }

    /// <summary>
    /// Gets the node at the specified pre-order index.
    /// </summary>
    /// <param name="preOrderIndex"></param>
    /// <returns></returns>
    public BehaviourNode GetNode(int preOrderIndex)
    {
      return allNodes[preOrderIndex];
    }

    public IReadOnlyList<BehaviourNode> AllNodes
    {
      get { return allNodes; }
    }

    /// <summary>
    /// Clear tree structure references.
    /// <list type="bullet">
    /// <item>Root</item>
    /// <item>References to parent Tree</item>
    /// <item>Parent-Child connections</item>
    /// <item>Internal Nodes List</item>
    /// </list>
    /// </summary>
    public void ClearStructure()
    {
      foreach (BehaviourNode node in allNodes)
      {
        ClearChildrenStructure(node);
        node.preOrderIndex = BehaviourNode.kInvalidOrder;
        node.indexOrder = 0;
        node.Parent = null;
        node.treeOwner = null;
      }

      allNodes.Clear();
    }

    private void ClearChildrenStructure(BehaviourNode node)
    {
      if (node.IsComposite())
      {
        var composite = node as Composite;
        composite.SetChildren(null);
      }

      else if (node.IsDecorator())
      {
        var decorator = node as Decorator;
        decorator.SetChild(null);
      }
    }

#if UNITY_EDITOR

    [ContextMenu("Add Blackboard")]
    void AddBlackboardAsset()
    {
      if (_blackboard == null && !EditorApplication.isPlaying)
      {
        _blackboard = CreateInstance<Blackboard>();
        _blackboard.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(_blackboard, this);
      }
    }

    [HideInInspector]
    public Vector2 panPosition = Vector2.zero;

    [HideInInspector]
    public Vector2 zoomPosition = Vector2.one;

    /// <summary>
    /// Unused nodes are nodes that are not part of the root.
    /// These are ignored when tree executes and excluded when cloning.
    /// </summary>
    [SerializeField, HideInInspector]
    public List<BehaviourNode> unusedNodes = new List<BehaviourNode>();

#endif

  }
}