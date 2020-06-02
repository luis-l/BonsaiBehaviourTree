namespace Bonsai.Core
{

  /// <summary>
  /// Interface for all nodes that can iterated.
  /// In order to not expose a list of children,
  /// classes implement two simple methods for the iterator to use.
  /// </summary>
  public interface IIterableNode<T>
  {
    /// <summary>
    /// Get the child at some index.
    /// This will allow the iterator to traverse in reverse
    /// the child list.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    T GetChildAt(int index);

    /// <summary>
    /// Get the child count. This tells the iterator to iterate
    /// from the indices [0, ChildCount() ).
    /// </summary>
    /// <returns></returns>
    int ChildCount();
  }
}
