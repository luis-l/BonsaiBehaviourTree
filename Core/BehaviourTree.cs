using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Bonsai.Standard;
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
    private Parallel[] parallelNodes;

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

      CacheObservers();
      CacheTreeTickNodes();
      SyncIterators();
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

    private void SyncIterators()
    {
      SyncParallelIterators();

      Root._iterator = mainIterator;

      BehaviourIterator itr = mainIterator;
      var parallelRoots = new Stack<BehaviourNode>();

      // This function handles assigning the iterator and skipping nodes.
      // The parallel root uses the same iterator as its parent, but the children
      // of the parallel node use their own iterator.
      Func<BehaviourNode, bool> skipAndAssign = (node) =>
      {
        node._iterator = itr;

        bool isParallel = node as Parallel != null;

        if (isParallel)
        {
          parallelRoots.Push(node);
        }

        return isParallel;
      };

      // Assign the main iterator to nodes not under any parallel nodes.
      TreeIterator<BehaviourNode>.Traverse(Root, delegate { }, skipAndAssign);

      while (parallelRoots.Count != 0)
      {
        BehaviourNode parallel = parallelRoots.Pop();

        // Do passes for each child, using the sub iterator associated with that child.
        for (int i = 0; i < parallel.ChildCount(); ++i)
        {
          itr = (parallel as Parallel).GetIterator(i);
          TreeIterator<BehaviourNode>.Traverse(parallel.GetChildAt(i), delegate { }, skipAndAssign);
        }
      }
    }

    private void SyncParallelIterators()
    {
      parallelNodes = GetNodes<Parallel>().ToArray();

      // Cache the parallel nodes and syn their iterators.
      foreach (Parallel p in parallelNodes)
      {
        p.SyncSubIterators();
      }
    }

    public void Interrupt(BehaviourNode subroot, bool bFullInterrupt = false)
    {
      // Interrupt this subtree.
      subroot.Iterator.StepBackInterrupt(subroot, bFullInterrupt);

      // Look for parallel nodes under the subroot.
      // Since the parallel count is usually small, we 
      // can just do a linear iteration to interrupt multiple
      // parallel nodes.
      for (int pIndex = 0; pIndex < parallelNodes.Length; ++pIndex)
      {
        Parallel p = parallelNodes[pIndex];

        if (IsUnderSubtree(subroot, p))
        {
          for (int itrIndex = 0; itrIndex < p.ChildCount(); ++itrIndex)
          {
            BehaviourIterator itr = p.GetIterator(itrIndex);

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

      // At this point the clone BT has its children in pre order order
      // and the original BT has pre-order indices calculated for each node.
      //
      // RELINK children and parents for the cloned nodes.
      // The clone node count is <= original node count because the editor may have dangling nodes.
      int maxCloneNodeCount = cloneBt.allNodes.Count;
      for (int i = 0; i < maxCloneNodeCount; ++i)
      {
        BehaviourNode nodeSource = sourceTree.allNodes[i];
        BehaviourNode copyNode = GetInstanceVersion(cloneBt, nodeSource);

        // When instantiating, the child list has references that point to the original.
        // Need to clear when adding the cloned children.
        copyNode.RemoveChildrenInternal();

        // Child count for this node.
        int childCount = nodeSource.ChildCount();

        // Start from one since child pre-order indices are after parent pre-order index.
        for (int childIndex = 0; childIndex < childCount; childIndex++)
        {
          BehaviourNode childSource = nodeSource.GetChildAt(childIndex);
          BehaviourNode copyChild = GetInstanceVersion(cloneBt, childSource);
          copyNode.AddChildOverride(copyChild);
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
        node.preOrderIndex = BehaviourNode.kInvalidOrder;
        node.indexOrder = 0;
        node.RemoveChildren();
        node.treeOwner = null;
      }

      allNodes.Clear();
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