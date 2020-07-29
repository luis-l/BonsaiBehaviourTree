using System.Collections.Generic;

namespace Bonsai.Utility
{
  /// <summary>
  /// Defers list modification so it is safe to traverse during an update call.
  /// </summary>
  public class UpdateList<T>
  {
    public readonly List<T> data = new List<T>();
    private readonly List<T> addQueue = new List<T>();
    private readonly List<T> removeQueue = new List<T>();

    /// <summary>
    /// Queues an item to add to the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
      addQueue.Add(item);
    }

    /// <summary>
    /// Queues an item for removal from the list.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    public void Remove(T item)
    {
      removeQueue.Add(item);
    }

    /// <summary>
    /// Removes and adds pending items in the modification queues.
    /// </summary>
    public void AddAndRemoveQueued()
    {
      if (removeQueue.Count != 0)
      {
        RemoveAll();
        removeQueue.Clear();
      }

      if (addQueue.Count != 0)
      {
        data.AddRange(addQueue);
        addQueue.Clear();
      }
    }

    // Reimplemented from List RemoveAll to avoid GC allocs from lambda closures.
    private void RemoveAll()
    {
      // The first free slot in items array
      int open = 0;
      int size = data.Count;

      // Find the first item which needs to be removed.
      while (open < size && !IsInRemovalQueue(data[open]))
      {
        open++;
      }

      if (open < size)
      {
        int current = open + 1;
        while (current < size)
        {
          // Find the first item which needs to be kept.
          while (current < size && IsInRemovalQueue(data[current]))
          {
            current++;
          }

          if (current < size)
          {
            // Copy item to the free slot.
            data[open++] = data[current++];
          }
        }
      }
    }

    private bool IsInRemovalQueue(T item)
    {
      return removeQueue.Contains(item);
    }
  }
}
