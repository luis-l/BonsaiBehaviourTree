using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using Bonsai.Standard;

namespace Bonsai.Core
{
  public class BehaviourTree : ScriptableObject
  {
    private BehaviourIterator _mainIterator;

    // Only conditional decorator nodes can have observer properties.
    private List<ConditionalAbort> _observerAborts;

    // Store references to the parallel nodes;
    private Parallel[] _parallelNodes;
    private int _parallelNodeCount = 0;

    [SerializeField, HideInInspector]
    private BehaviourNode _root;

    private bool _bTreeInitialized = false;

    /// <summary>
    /// The game object binded to the tree.
    /// This is assigned at runtime when the tree instance starts.
    /// </summary>
    public GameObject parentGameObject;

    /// <summary>
    /// Gets and sets the tree root.
    /// </summary>
    public BehaviourNode Root
    {
      get { return _root; }
      set
      {
        // NOTE:
        // Everytime we set the root, Start()
        // must be called again in order to preprocess the tree.
        _bTreeInitialized = false;

        if (value == null)
        {
          Debug.LogWarning("Cannot initialize with null node");
          return;
        }

        // Setup root.
        if (value.Parent == null)
        {
          _root = value;
        }

        else
        {
          Debug.LogWarning("Cannot set parented node as tree root.");
        }
      }
    }

    [SerializeField, HideInInspector]
    private Blackboard _blackboard;

    public Blackboard Blackboard
    {
      get { return _blackboard; }
    }

    [SerializeField, HideInInspector]
    internal List<BehaviourNode> allNodes = new List<BehaviourNode>();

    public void SetBlackboard(Blackboard bb)
    {
      _blackboard = bb;
    }

    /// <summary>
    /// Preprocesses and starts the tree.
    /// </summary>
    /// <param name="root"></param>
    public void Start()
    {
      if (_root == null)
      {
        Debug.LogWarning("Cannot start tree with a null root.");
        return;
      }

      preProcess();

      for (int i = 0; i < allNodes.Count; ++i)
      {
        allNodes[i].OnStart();
      }

      _mainIterator.Traverse(_root);
      _bTreeInitialized = true;
    }

    public void Update()
    {
      if (_bTreeInitialized && _mainIterator.IsRunning)
      {

        if (_observerAborts.Count != 0)
        {
          tickObservers();
        }

        _mainIterator.Update();
      }
    }

    /// <summary>
    /// Processes the tree to calculate certain properties like node priorities,
    /// caching observers, and syncinc parallel iterators.
    /// The root must be set.
    /// </summary>
    private void preProcess()
    {
      if (_root == null)
      {
        Debug.Log("The tree must have a valid root in order to be pre-processed");
        return;
      }

      CalculateTreeOrders();

      _mainIterator = new BehaviourIterator(this, 0);

      // Setup a new list for the observer nodes.
      _observerAborts = new List<ConditionalAbort>();

      cacheObservers();
      syncIterators();
    }

    private void cacheObservers()
    {
      _observerAborts.Clear();

      foreach (var conditional in GetNodes<ConditionalAbort>())
      {
        if (conditional.abortType != AbortType.None)
        {
          _observerAborts.Add(conditional);
        }
      }
    }

    private void syncIterators()
    {
      syncParallelIterators();

      _root._iterator = _mainIterator;

      BehaviourIterator itr = _mainIterator;
      var parallelRoots = new Stack<BehaviourNode>();

      // This function handles assigning the iterator and skipping nodes.
      // The parallel root uses the same iterator as its parent, but the children
      // of the parallel node use their own iterator.
      Func<BehaviourNode, bool> skipAndAssign = (node) =>
      {
        node._iterator = itr;

        bool bIsParallel = node as Parallel != null;

        if (bIsParallel)
        {
          parallelRoots.Push(node);
        }

        return bIsParallel;
      };

      Action<BehaviourNode> nothing = (node) => { };

      // Assign the main iterator to nodes not under any parallel nodes.
      TreeIterator<BehaviourNode>.Traverse(_root, nothing, skipAndAssign);

      while (parallelRoots.Count != 0)
      {

        BehaviourNode parallel = parallelRoots.Pop();

        // Do passes for each child, using the sub iterator associated with that child.
        for (int i = 0; i < parallel.ChildCount(); ++i)
        {

          itr = (parallel as Parallel).GetIterator(i);
          TreeIterator<BehaviourNode>.Traverse(parallel.GetChildAt(i), nothing, skipAndAssign);
        }
      }
    }

    private void syncParallelIterators()
    {
      var parallelNodes = GetNodes<Parallel>();
      _parallelNodeCount = parallelNodes.Count;

      if (_parallelNodeCount > 0)
      {

        _parallelNodes = new Parallel[_parallelNodeCount];

        // Cache the parallel nodes and syn their iterators.
        int i = 0;
        foreach (Parallel p in parallelNodes)
        {

          _parallelNodes[i++] = p;
          p.SyncSubIterators();
        }
      }
    }

    public void Interrupt(BehaviourNode subroot, bool bFullInterrupt = false)
    {
      // Interrupt this subtree.
      subroot.Iterator.stepBackInterrupt(subroot, bFullInterrupt);

      // Look for parallel nodes under the subroot.
      // Since the parallel count is usually small, we 
      // can just do a linear iteration to interrupt multiple
      // parallel nodes.
      for (int pIndex = 0; pIndex < _parallelNodeCount; ++pIndex)
      {
        Parallel p = _parallelNodes[pIndex];

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

              itr.stepBackInterrupt(firstNode.Parent, bFullInterrupt);
            }
          }
        }
      }
    }

    /// <summary>
    /// Test to see if a node is a parallel node.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public bool IsParallelNode(BehaviourNode node)
    {
      return _parallelNodeCount != 0 && containsParallelPreOrder(node.preOrderIndex);
    }

    // Here we use a simple linear iteration over the array containing pre-orders for parallel nodes.
    // We do a linear search since the number of parallel node is usually small.
    private bool containsParallelPreOrder(int preOrderIndex)
    {
      for (int i = 0; i < _parallelNodeCount; ++i)
      {
        if (_parallelNodes[i].preOrderIndex == preOrderIndex)
        {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Computes the pre and post orders of all nodes.
    /// </summary>
    public void CalculateTreeOrders()
    {
      ResetOrderIndices();

      int orderCounter = 0;

      Action<BehaviourNode> preOrder = (node) =>
      {
        node.preOrderIndex = orderCounter++;
      };

      TreeIterator<BehaviourNode>.Traverse(_root, preOrder);

      orderCounter = 0;


      Action<BehaviourNode> postOrder = (node) =>
      {
        node.postOrderIndex = orderCounter++;
      };

      TreeIterator<BehaviourNode>.Traverse(_root, postOrder, Traversal.PostOrder);


      Action<BehaviourNode, TreeIterator<BehaviourNode>> levelOrder = (node, itr) =>
      {
        node.levelOrder = itr.CurrentLevel;

        // This will end up with the highest level.
        Height = itr.CurrentLevel;
      };

      TreeIterator<BehaviourNode>.Traverse(_root, levelOrder, Traversal.LevelOrder);
    }

    /// <summary>
    /// Gets the nodes of type T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public List<T> GetNodes<T>() where T : BehaviourNode
    {
      var nodes = new List<T>();

      for (int i = 0; i < allNodes.Count; ++i)
      {

        var node = allNodes[i] as T;

        if (node)
        {
          nodes.Add(node);
        }
      }

      return nodes;
    }

    // Note on multiple aborts:
    // If there are multiple satisfied aborts, then
    // the tree picks the highest order abort (left most).
    private void tickObservers()
    {
      for (int i = 0; i < _observerAborts.Count; ++i)
      {
        ConditionalAbort node = _observerAborts[i];

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

    /// <summary>
    /// Tests if the order of a is lower than b.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool isLowerOrder(int orderA, int orderB)
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
    public static bool isHigherOrder(int orderA, int orderB)
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
      return _mainIterator != null && _mainIterator.IsRunning;
    }

    public BehaviourNode.Status LastStatus()
    {
      return _mainIterator.LastStatusReturned;
    }

    public int Height { get; private set; } = 0;

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
    /// Make sure that the original behaviour tree has its pre-orders calculated.
    /// </summary>
    /// <param name="originalBT"></param>
    /// <returns></returns>
    public static BehaviourTree Clone(BehaviourTree originalBT)
    {
      var cloneBt = Instantiate(originalBT);
      cloneBt._blackboard = Instantiate(originalBT._blackboard);

      cloneBt.allNodes.Clear();

      Action<BehaviourNode> copier = (originalNode) =>
      {
        var nodeCopy = Instantiate(originalNode);

        // Linke the root copy.
        if (originalBT.Root == originalNode)
        {
          cloneBt.Root = nodeCopy;
        }

        // Nodes will be added in pre-order.
        nodeCopy.ClearTree();
        nodeCopy.Tree = cloneBt;
      };

      // Traversing in tree order will make sure that the runtime tree has its nodes properly sorted
      // in pre-order and will also make sure that dangling nodes are left out (unconnected nodes from the editor).
      TreeIterator<BehaviourNode>.Traverse(originalBT.Root, copier);

      // At this point the clone BT has its children in pre order order
      // and the original BT has pre-order indices calculated for each node.
      //
      // RELINK children and parent associations of the cloned nodes.
      // The clone node count is <= original node count because the editor may have dangling nodes.
      int maxCloneNodeCount = cloneBt.allNodes.Count;
      for (int i = 0; i < maxCloneNodeCount; ++i)
      {

        BehaviourNode originalNode = originalBT.allNodes[i];
        BehaviourNode originalParent = originalNode.Parent;

        if (originalParent)
        {

          BehaviourNode copyNode = GetInstanceVersion(cloneBt, originalNode);
          BehaviourNode copyParent = GetInstanceVersion(cloneBt, originalParent);

          copyParent.ForceSetChild(copyNode);
        }
      }

      for (int i = 0; i < maxCloneNodeCount; ++i)
      {
        cloneBt.allNodes[i].OnCopy();
      }

      IncludeTreeReferences(cloneBt);

      return cloneBt;
    }

    public static void IncludeTreeReferences(BehaviourTree mainTree)
    {
      var includes = mainTree.GetNodes<Include>();

      // Nothing to include.
      if (includes.Count == 0)
      {
        return;
      }

      var includedTrees = new BehaviourTree[includes.Count];

      for (int i = 0; i < includes.Count; ++i)
      {

        // Clone each individual tree independently.
        Include include = includes[i];
        BehaviourTree subtree = Clone(include.tree);

        BehaviourNode includeParent = include.Parent;
        BehaviourNode subtreeRoot = subtree.Root;

        if (includeParent)
        {

          // The root node must now be the child of the parent of the include.
          subtreeRoot._indexOrder = include._indexOrder;
          includeParent.ForceSetChild(subtreeRoot);
        }

        else if (include.preOrderIndex == 0)
        {

          // If the include node is the root, then just make the subtree the root.
          mainTree.Root = subtreeRoot;
        }

#if UNITY_EDITOR
        Vector2 deltaFromSubrootToInclude = include.bonsaiNodePosition - subtreeRoot.bonsaiNodePosition;
#endif

        // The nodes of the included tree, must now reference the main tree.
        foreach (BehaviourNode b in subtree.AllNodes)
        {
          b.ClearTree();
          b.Tree = mainTree;

#if UNITY_EDITOR
          // Offset the sub tree nodes so they are placed under the include node.
          b.bonsaiNodePosition += deltaFromSubrootToInclude;
#endif
        }
      }

      // After everything is included, we need to resort the tree nodes in pre order with the new included nodes.
      mainTree.SortNodes();

      // Destory the cloned subtrees since we do not need them anymore.
      for (int i = 0; i < includedTrees.Length; ++i)
      {
        Destroy(includedTrees[i]);
      }

      // Destroy the includes
      for (int i = 0; i < includes.Count; ++i)
      {
        Destroy(includes[i]);
      }
    }

    /// <summary>
    /// Sorts the nodes in pre order.
    /// </summary>
    public void SortNodes()
    {
      CalculateTreeOrders();

      // Moves back the dangling nodes to the end of the list and then
      // sorts the nodes by pre-order.
      allNodes = allNodes
          .OrderBy(node => node.preOrderIndex == BehaviourNode.kInvalidOrder)
          .ThenBy(node => node.preOrderIndex)
          .ToList();
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

    public IEnumerable<BehaviourNode> AllNodes
    {
      get { return allNodes; }
    }

    /// <summary>
    /// Resets the pre and post order indices.
    /// </summary>
    public void ResetOrderIndices()
    {
      foreach (BehaviourNode b in allNodes)
      {
        b.preOrderIndex = BehaviourNode.kInvalidOrder;
        b.postOrderIndex = BehaviourNode.kInvalidOrder;
        b.levelOrder = BehaviourNode.kInvalidOrder;
      }
    }

    public void SetRandomSeed(int seed)
    {
      Random = new System.Random(seed);
    }

    public System.Random Random { get; private set; } = new System.Random();

#if UNITY_EDITOR

    public void OnDrawGizmos()
    {
      foreach (BehaviourNode b in allNodes)
      {
        b.OnDrawGizmos();
      }
    }

    private void drawGizmos(BehaviourNode n)
    {
      n.OnDrawGizmos();
    }

    #region Node Editor Meta Data

    [HideInInspector]
    public Vector2 panPosition = Vector2.zero;

    [HideInInspector]
    public Vector2 zoomPosition = Vector2.one;

    #endregion
#endif

  }
}