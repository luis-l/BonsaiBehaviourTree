
using System;
using System.Collections.Generic;

namespace Bonsai.Core
{
  public static class TreeTraversal
  {
    public static IEnumerable<T> PreOrder<T>(T root) where T : IIterableNode<T>
    {
      var stack = new Stack<T>();

      if (root != null)
      {
        stack.Push(root);
      }

      while (stack.Count != 0)
      {
        var top = stack.Pop();
        yield return top;
        for (int i = top.ChildCount() - 1; i >= 0; --i)
        {
          T child = top.GetChildAt(i);
          stack.Push(child);
        }
      }
    }

    public static IEnumerable<T> PreOrderSkipChildren<T>(T root, Predicate<T> skip) where T : IIterableNode<T>
    {
      var stack = new Stack<T>();

      if (root != null)
      {
        stack.Push(root);
      }

      while (stack.Count != 0)
      {
        var top = stack.Pop();
        yield return top;

        // Children for this node should not be ignored.
        if (!skip(top))
        {
          for (int i = top.ChildCount() - 1; i >= 0; --i)
          {
            T child = top.GetChildAt(i);
            stack.Push(child);
          }
        }
      }
    }

    public static IEnumerable<T> PostOrder<T>(T root) where T : IIterableNode<T>
    {
      if (root != null)
      {
        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        stack.Push(root);

        while (stack.Count != 0)
        {
          T current = stack.Peek();

          // Keep pushing until we reach a leaf.
          // Also do not re-traverse nodes that already had their children added.
          while (!visited.Contains(current) && current.ChildCount() != 0)
          {
            for (int i = current.ChildCount() - 1; i >= 0; --i)
            {
              T child = current.GetChildAt(i);
              stack.Push(child);
            }

            visited.Add(current);
            current = stack.Peek();
          }

          yield return stack.Pop();
        }
      }
    }

    /// <summary>
    /// Traverse the tree in level order.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="root"></param>
    /// <returns>The node along with the current level of the node in the tree.</returns>
    public static IEnumerable<ValueTuple<T, int>> LevelOrder<T>(T root) where T : IIterableNode<T>
    {
      // The current level that the tree is currently in (For level order traversal only).
      int currentLevel = 0;

      // Used in level traversal to know when we reached a new tree level.
      int queueNodeCount = 0;

      var queue = new Queue<T>();

      if (root != null)
      {
        queue.Enqueue(root);
      }

      while (queue.Count != 0)
      {
        // Keep dequeuing from the current level.
        if (queueNodeCount > 0)
        {
          queueNodeCount -= 1;
        }

        // Once we dequeued the entire level, we go down a level.
        if (queueNodeCount == 0)
        {
          queueNodeCount = queue.Count;
          currentLevel += 1;
        }

        var top = queue.Dequeue();

        // Current level offset by -1 so it matches the start of arrays.
        yield return new ValueTuple<T, int>(top, currentLevel - 1);

        for (int i = 0; i < top.ChildCount(); ++i)
        {
          T child = top.GetChildAt(i);
          queue.Enqueue(child);
        }
      }
    }

  }
}
