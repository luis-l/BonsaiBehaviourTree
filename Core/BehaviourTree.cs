﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Core
{
  [CreateAssetMenu(fileName = "BonsaiBT", menuName = "Bonsai/Behaviour Tree")]
  public class BehaviourTree : ScriptableObject
  {
    // The iterator that ticks branches under the tree root.
    // Does not tick branches under parallel nodes since those use their own parallel iterators.
    private BehaviourIterator mainIterator;

    // Only conditional decorator nodes can have observer properties.
    private List<ConditionalAbort> observerAborts;

    // Store references to the parallel nodes.
    private ParallelComposite[] parallelNodes;

    // Nodes that are allowed to update on tree tick.
    private BehaviourNode[] treeTickNodes;

    // Flags if the tree has been initialized and is ready to run.
    // This is set on tree Start. 
    // If false, the tree will not be traversed for running.
    private bool isTreeInitialized = false;

    // allNodes must always be kept in pre-order.
    [SerializeField, HideInInspector]
#pragma warning disable IDE0044 // Add readonly modifier
    private BehaviourNode[] allNodes = { };
#pragma warning restore IDE0044 // Add readonly modifier

    /// <summary>
    /// The nodes in the tree in pre-order.
    /// </summary>
    public BehaviourNode[] Nodes { get { return allNodes; } }

    [SerializeField, HideInInspector]
    public Blackboard blackboard;

    /// <summary>
    /// The first node in the tree. 
    /// Also the entry point to run the tree.
    /// </summary>
    public BehaviourNode Root
    {
      get { return allNodes.Length == 0 ? null : allNodes[0]; }
    }

    /// <summary>
    /// <para>The game object actor associated with the tree.</para>
    /// <para>Field is optional. The tree core can run without the actor.</para>
    /// </summary>
    public GameObject actor;

    /// <summary>
    /// The maximum height of the tree. 
    /// This is the height measured from the root to the furthest leaf.
    /// </summary>
    public int Height { get; private set; } = 0;

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

    /// <summary>
    /// Ticks (steps) the tree once.
    /// The tree must be started beforehand.
    /// <seealso cref="Start"/>
    /// </summary>
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
    /// Sets the tree nodes. Must be in pre-order.
    /// </summary>
    /// <param name="node">The nodes in pre-order.</param>
    public void SetNodes(IEnumerable<BehaviourNode> nodes)
    {
      allNodes = nodes.ToArray();
      int preOrderIndex = 0;
      foreach (BehaviourNode node in allNodes)
      {
        node.treeOwner = this;
        node.preOrderIndex = preOrderIndex++;
      }
    }

    /// <summary>
    /// Traverses the nodes under the root in pre-order and sets the tree nodes.
    /// </summary>
    /// <param name="root">The tree root</param>
    public void SetNodes(BehaviourNode root)
    {
      SetNodes(TreeTraversal.PreOrder(root));
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
    /// Gets the nodes of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<T> GetNodes<T>() where T : BehaviourNode
    {
      return allNodes.Select(node => node as T).Where(casted => casted != null);
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
      foreach (BehaviourNode node in TreeTraversal.PreOrderSkipChildren(Root, n => n is ParallelComposite))
      {
        node.Iterator = mainIterator;
      }
    }

    /// <summary>
    /// Sets the nodes post and level order numbering.
    /// </summary>
    private void SetPostandLevelOrders()
    {
      int postOrderIndex = 0;
      foreach (BehaviourNode node in TreeTraversal.PostOrder(Root))
      {
        node.postOrderIndex = postOrderIndex++;
      }

      foreach ((BehaviourNode node, int level) in TreeTraversal.LevelOrder(Root))
      {
        node.levelOrder = level;
        Height = level;
      }
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

      if (sourceTree.blackboard)
      {
        cloneBt.blackboard = Instantiate(sourceTree.blackboard);
      }

      // Source tree nodes should already be in pre-order.
      cloneBt.SetNodes(sourceTree.Nodes.Select(n => Instantiate(n)));

      // Relink children and parents for the cloned nodes.
      int maxCloneNodeCount = cloneBt.allNodes.Length;
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

      allNodes = new BehaviourNode[] { };
    }

    private void ClearChildrenStructure(BehaviourNode node)
    {
      if (node.IsComposite())
      {
        var composite = node as Composite;
        composite.SetChildren(new BehaviourNode[] { });
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
      if (blackboard == null && !EditorApplication.isPlaying)
      {
        blackboard = CreateInstance<Blackboard>();
        blackboard.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(blackboard, this);
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