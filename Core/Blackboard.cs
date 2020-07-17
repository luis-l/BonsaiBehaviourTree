using System.Collections.Generic;
using UnityEngine;

namespace Bonsai.Core
{
  ///<summary>
  /// A heterogeneous dictionary to store shared data for a BehaviourTree.
  ///</summary>
  public class Blackboard : ScriptableObject, ISerializationCallbackReceiver
  {
    private readonly Dictionary<string, object> memory = new Dictionary<string, object>();

    /// <summary>
    /// The internal memory of the blackboard.
    /// </summary>
    public IReadOnlyDictionary<string, object> Memory
    {
      get { return memory; }
    }

    // Used to serailize the key names.
    // Note: Cannot be readonly since it will not serialize in the ScriptableObject.
    [SerializeField, HideInInspector]
#pragma warning disable IDE0044 // Add readonly modifier
    private List<string> keys = new List<string>();
#pragma warning restore IDE0044 // Add readonly modifier

    ///<summary>
    /// Sets key in the blackboard with an unset value.
    ///</summary>
    public void Set(string key)
    {
      if (!memory.ContainsKey(key))
      {
        memory.Add(key, null);
      }
    }

    /// <summary>
    /// Set the blackboard key to a value.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, object value)
    {
      memory[key] = value;
    }

    ///<summary>
    /// Get the key value.
    ///</summary>
    /// <returns>
    /// <para>Key value of type T if it exists in the Blackboard and is set.</para>
    /// default(T) if it does not exist or is unset.
    /// </returns>
    public T Get<T>(string key)
    {
      object value = Get(key);
      if (value == null)
      {
        return default;
      }

      return (T)value;
    }

    /// <summary>
    /// Get the key value.
    /// </summary>
    /// <returns>Value at the key. Null if it is unset or if the Blackboard does not contain key.</returns>
    public object Get(string key)
    {
      if (Contains(key))
      {
        return memory[key];
      }

      return null;
    }

    ///<summary>
    /// Removes the key from the Blackboard.
    ///</summary>
    public void Remove(string key)
    {
      memory.Remove(key);
    }

    /// <summary>
    /// Sets the key value to null. Key must exist in the Blackboard.
    /// </summary>
    public void Unset(string key)
    {
      if (Contains(key))
      {
        memory[key] = null;
      }
    }

    /// <summary>
    /// Removes all keys from the Blackboard.
    /// </summary>
    public void Clear()
    {
      memory.Clear();
    }

    /// <summary>
    /// Check if the key exists in the Blackboard.
    /// </summary>
    public bool Contains(string key)
    {
      return memory.ContainsKey(key);
    }

    /// <summary>
    /// Does the key exist and is the value not null?
    /// </summary>
    public bool IsSet(string key)
    {
      return Contains(key) && Get(key) != null;
    }

    /// <summary>
    /// Does the key exist is the value null?
    /// </summary>
    public bool IsUnset(string key)
    {
      return Contains(key) && Get(key) == null;
    }

    /// <summary>
    /// The number of keys in the Blackboard.
    /// </summary>
    public int Count
    {
      get { return memory.Count; }
    }

    // Sets all Blackboard keys with unset values.
    public void OnAfterDeserialize()
    {
      memory.Clear();

      foreach (string key in keys)
      {
        memory.Add(key, null);
      }
    }

    // Collects all current Blackboard keys for serialization as a List.
    public void OnBeforeSerialize()
    {
      keys.Clear();
      foreach (string key in memory.Keys)
      {
        keys.Add(key);
      }
    }
  }
}
