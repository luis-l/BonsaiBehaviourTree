using System;
using System.Collections.Generic;

namespace Bonsai.Core
{
  public enum Traversal { PreOrder, PostOrder, LevelOrder };
  public enum TraversalSkip { None, Root }

  public class TreeIterator<T> where T : IIterableNode<T>
  {
    // For Pre and Post order.
    private readonly Stack<T> stackPath;

    // For level order.
    private readonly Queue<T> queuePath;

    // Used for PostOrder
    private readonly HashSet<T> visited;

    // The type of traversal the iterator is doing.
    private readonly Traversal traversal;

    // The current level that the tree is currently in (For level order traversal only).
    private int currentLevel = 0;

    // Used in level traversal to know when we reached a new tree level.
    private int queueNodeCount = 0;

    private Func<T, bool> SkipFilter;

    public TreeIterator(T root, Traversal type = Traversal.PreOrder)
    {
      if (root == null)
      {
        return;
      }

      traversal = type;

      if (type == Traversal.LevelOrder)
      {
        queuePath = new Queue<T>();
        queuePath.Enqueue(root);
      }

      else
      {
        stackPath = new Stack<T>();
        stackPath.Push(root);

        if (type == Traversal.PostOrder)
        {
          visited = new HashSet<T>();
        }
      }
    }

    /// <summary>
    /// Iterates and returns the next node in the tree.
    /// </summary>
    /// <returns></returns>
    public T Next()
    {
      switch (traversal)
      {
        case Traversal.PreOrder:
          return PreOrderNext();

        case Traversal.PostOrder:
          return PostOrderNext();

        default: return LevelOrderNext();
      }
    }

    private T PreOrderNext()
    {
      T current = stackPath.Pop();

      for (int i = current.ChildCount() - 1; i >= 0; --i)
      {
        T child = current.GetChildAt(i);

        if (SkipFilter != null && SkipFilter(child))
        {
          continue;
        }

        stackPath.Push(child);
      }

      return current;
    }

    private T PostOrderNext()
    {
      T current = stackPath.Peek();

      // Keep pushing until we reach a leaf.
      // Also do not re-traverse nodes that already had their children added.
      while (!visited.Contains(current) && current.ChildCount() != 0)
      {

        for (int i = current.ChildCount() - 1; i >= 0; --i)
        {
          T child = current.GetChildAt(i);

          stackPath.Push(child);
        }

        visited.Add(current);
        current = stackPath.Peek();
      }

      return stackPath.Pop();
    }

    private T LevelOrderNext()
    {
      // Keep dequeuing from the current level.
      if (queueNodeCount > 0)
      {
        queueNodeCount -= 1;
      }

      // Once we dequeued the entire level, we go down a level.
      if (queueNodeCount == 0)
      {
        // Don't forget to adjust for skipping from filter in order
        // to keep the proper level.
        queueNodeCount = queuePath.Count;
        currentLevel += 1;
      }

      T current = queuePath.Dequeue();

      for (int i = 0; i < current.ChildCount(); ++i)
      {
        var child = current.GetChildAt(i);
        queuePath.Enqueue(child);
      }

      return current;
    }

    /// <summary>
    /// Checks if there are still nodes to traverse.
    /// </summary>
    /// <returns></returns>
    public bool HasNext()
    {
      if (stackPath != null) return stackPath.Count != 0;
      if (queuePath != null) return queuePath.Count != 0;
      return false;
    }

    /// <summary>
    /// The current level of the tree that the iterator is in.
    /// It is offset by -1 so it matches the start of arrays.
    /// NOTE: Only works with level traversals. 
    /// </summary>
    public int CurrentLevel
    {
      get { return currentLevel - 1; }
    }

    /// <summary>
    /// A helper method to traverse all nodes and execute an action per node.
    /// </summary>
    /// <param name="root">The travseral start.</param>
    /// <param name="onNext">The action to execute per node.</param>
    /// <param name="traversal">The type of DFS traversal.</param>
    public static void Traverse(
      T root,
      Action<T> onNext,
      Traversal traversal = Traversal.PreOrder,
      TraversalSkip skip = TraversalSkip.None)
    {
      var itr = new TreeIterator<T>(root, traversal);

      if (skip == TraversalSkip.Root)
      {
        itr.Next();
      }

      while (itr.HasNext())
      {
        var node = itr.Next();
        onNext(node);
      }
    }

    /// <summary>
    /// A helper method to traverse all nodes and execute an action per node.
    /// This method also passes the iterator doing the traversal.
    /// </summary>
    /// <param name="root">The traversal start.</param>
    /// <param name="onNext">The action to execute per node.</param>
    /// <param name="traversal">The type of DFS traversal.</param>
    public static void Traverse(T root, Action<T, TreeIterator<T>> onNext, Traversal traversal = Traversal.PreOrder)
    {
      var itr = new TreeIterator<T>(root, traversal);

      while (itr.HasNext())
      {
        var node = itr.Next();
        onNext(node, itr);
      }
    }


    /// <summary>
    /// A helper method to traverse all nodes and accumulate a value over the traversal.
    /// </summary>
    /// <typeparam name="TAccum"></typeparam>
    /// <param name="root"></param>
    /// <param name="accumulator">The function used to accumulate the value</param>
    /// <param name="initial">The starting value for the accumulation</param>
    /// <param name="traversal"></param>
    /// <returns></returns>
    public static TAccum Traverse<TAccum>(T root, Func<TAccum, T, TAccum> accumulator, TAccum initial, Traversal traversal = Traversal.PreOrder)
    {
      var itr = new TreeIterator<T>(root, traversal);

      while (itr.HasNext())
      {
        var node = itr.Next();
        initial = accumulator(initial, node);
      }

      return initial;

    }

    /// <summary>
    /// A pre-order traversal with the option to skip some nodes.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="onNext"></param>
    /// <param name="skipFilter"></param>
    public static void Traverse(T root, Action<T> onNext, Func<T, bool> skipFilter)
    {
      if (skipFilter(root))
      {
        return;
      }

      var itr = new TreeIterator<T>(root)
      {
        SkipFilter = skipFilter
      };

      while (itr.HasNext())
      {
        var node = itr.Next();
        onNext(node);
      }
    }
  }
}
