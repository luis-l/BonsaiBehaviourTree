namespace Bonsai.Utility
{
  public class FixedSizeStack<T>
  {
    private readonly T[] _container;

    public FixedSizeStack(int length)
    {
      Count = 0;
      _container = new T[length];
    }

    /// <summary>
    /// Reset count and set all entries to the default T value.
    /// </summary>
    public void Clear()
    {
      Count = 0;
      for (int i = 0; i < _container.Length; ++i)
      {
        _container[i] = default;
      }
    }

    /// <summary>
    /// Reset the count. Entries are left as is.
    /// </summary>
    public void ResetCount()
    {
      Count = 0;
    }

    public T Peek()
    {
      return _container[Count - 1];
    }

    public T Pop()
    {
      return _container[--Count];
    }

    public void Push(T value)
    {
      _container[Count++] = value;
    }

    public int Count { get; private set; }

    public T GetValue(int index)
    {
      return _container[index];
    }
  }
}