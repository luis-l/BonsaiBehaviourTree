using System;
using System.Collections.Generic;

namespace Bonsai.Utility
{
  /// <summary>
  /// Defers list modification so it is safe to traverse during an update call.
  /// </summary>
  public class UpdateList<T>
  {
    private readonly List<T> data = new List<T>();
    private readonly List<T> addQueue = new List<T>();
    private readonly List<T> removeQueue = new List<T>();

    private readonly Predicate<T> IsInRemovalQueue;

    public IReadOnlyList<T> Data { get { return data; } }

    public UpdateList()
    {
      IsInRemovalQueue = delegate (T value)
      {
        return removeQueue.Contains(value);
      };
    }

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
        data.RemoveAll(IsInRemovalQueue);
        removeQueue.Clear();
      }

      if (addQueue.Count != 0)
      {
        data.AddRange(addQueue);
        addQueue.Clear();
      }
    }
  }
}
