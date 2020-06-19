
using System;

namespace Bonsai.Utility
{
  /// <summary>
  /// Raises an event whenever the value is set.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ReactiveValue<T>
  {
    private T value;

    public event EventHandler<T> ValueChanged;

    public T Value
    {
      get { return value; }
      set
      {
        this.value = value;
        OnValueChanged();
      }
    }

    public ReactiveValue()
    {

    }

    public ReactiveValue(T value)
    {
      this.value = value;
    }

    protected virtual void OnValueChanged()
    {
      ValueChanged?.Invoke(this, value);
    }
  }
}
