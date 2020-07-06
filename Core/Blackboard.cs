
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using Bonsai.Utility;

namespace Bonsai.Core
{
  ///<summary>
  /// A generic data container where a datum is associated with a key.
  ///</summary>
  public class Blackboard : ScriptableObject, ISerializationCallbackReceiver
  {
    private Dictionary<string, RegisterBase> memory = new Dictionary<string, RegisterBase>();

    /// <summary>
    /// The dictionary containing the string-Register pairs.
    /// </summary>
    public IEnumerable<KeyValuePair<string, RegisterBase>> Memory
    {
      get
      {
        return memory;
      }
    }

    // Used to serailize the register names.
    [SerializeField, HideInInspector]
    private readonly List<string> keys = new List<string>();

    // Used to serialize the register type.
    [SerializeField, HideInInspector]
    private readonly List<string> types = new List<string>();

    ///<summary>
    /// Adds a register to the Blackboard.
    ///</summary>
    public void Add<T>(string key)
    {
      if (memory.ContainsKey(key))
      {
        Debug.LogWarning("Cannot Add. Register " + key + " already exists in the Blackboard.");
      }

      else
      {
        var r = new Register<T>();
        memory.Add(key, r);
      }
    }

    /// <summary>
    /// Add a register and pass the initial value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Add<T>(string key, T value)
    {
      Add<T>(key);

      if (Exists(key))
      {
        Set(key, value);
      }
    }

    /// <summary>
    /// For Editor use only.
    /// Creates a register of the specified type via Reflection.
    /// </summary>
    public void Add(string key, Type t)
    {
      if (memory.ContainsKey(key))
      {
        Debug.LogWarning("Cannot Add. Register " + key + " already exists in the Blackboard.");
      }

      else
      {

        var genericRegType = typeof(Register<>);
        Type[] args = { t };

        Type registerType = genericRegType.MakeGenericType(args);
        var register = Activator.CreateInstance(registerType) as RegisterBase;

        memory.Add(key, register);
      }
    }

    ///<summary>
    /// Removes the register.
    ///</summary>
    public void Remove(string key)
    {
      memory.Remove(key);
    }

    public void Clear()
    {
      memory.Clear();
    }

    /// <summary>
    /// Sets the value of the register in the blackboard.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The name of the register.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="forceAdd">Should the blackboard add a key if it does not exist?</param>
    public void Set<T>(string key, T value, bool forceAdd = false)
    {
      if (forceAdd && !Exists(key))
      {
        Add<T>(key);
      }

      var register = GetRegister<T>(key);

      if (register != null)
      {
        register.value = value;
      }
    }

    ///<summary>
    /// Gets the value of the register. If it fails it returns default(T).
    ///</summary>
    public T Get<T>(string key)
    {
      var register = GetRegister<T>(key);

      if (register != null)
      {
        return register.value;
      }

      return default;
    }

    /// <summary>
    /// Gets the value of the register as an object type.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object Get(string key)
    {
      return memory[key].GetValue();
    }

    public RegisterBase GetRegister(string key)
    {
      return memory[key];
    }

    /// <summary>
    /// Gets the register at the specified key.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public Register<T> GetRegister<T>(string key)
    {
      if (!Exists(key))
      {
        return null;
      }

      var register = memory[key] as Register<T>;

      if (register == null)
      {
        Debug.LogError("Type mismatch for register: " + key + ". Type requested: " + typeof(T));
      }

      return register;
    }

    // Test if the key exists in the memory dictionary.
    public bool Exists(string key)
    {
      return memory.ContainsKey(key);
    }

    /// <summary>
    /// Returns the register count.
    /// </summary>
    public int Count
    {
      get { return memory.Count; }
    }

    /// <summary>
    /// The base class for all registers. This allows to have registers
    /// of different types in the same dictionary.
    /// </summary>
    [Serializable]
    public abstract class RegisterBase
    {
      private FieldInfo _valueField;
      private FieldInfo _valueTypeField;

      /// <summary>
      /// Gets the field info of the value.
      /// </summary>
      public FieldInfo ValueField
      {
        get
        {
          if (_valueField == null)
          {
            _valueField = GetType().GetField("value");
          }

          return _valueField;
        }
      }

      /// <summary>
      /// Gets the field info of the value type.
      /// </summary>
      public FieldInfo ValueTypeField
      {
        get
        {
          if (_valueTypeField == null)
          {
            _valueTypeField = GetType().GetField("valueType");
          }

          return _valueTypeField;
        }
      }

      /// <summary>
      /// Get the value of the register via the reflected FieldInfo.
      /// </summary>
      /// <returns></returns>
      public object GetValue()
      {
        return ValueField.GetValue(this);
      }

      /// <summary>
      /// Get the type of the value in the register via reflected FieldInfo.
      /// </summary>
      /// <returns></returns>
      public Type GetValueType()
      {
        return ValueTypeField.GetValue(this) as Type;
      }

      /// <summary>
      /// Set the value of the register via the reflected FieldInfo.
      /// </summary>
      /// <param name="o"></param>
      public void SetValue(object o)
      {
        ValueField.SetValue(this, o);
      }
    }

    /// <summary>
    /// The register containing a value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class Register<T> : RegisterBase
    {
      public T value;
      public readonly Type valueType = typeof(T);
    }

    public void OnAfterDeserialize()
    {
      memory = new Dictionary<string, RegisterBase>();

      if (keys.Count != types.Count)
      {
        Debug.LogError("Serialization failure. The lengths of the key and type lists are not the same.");
      }

      int length = Math.Min(keys.Count, types.Count);

      for (int i = 0; i < length; ++i)
      {

        string key = keys[i];
        Type type = Type.GetType(types[i]);

        Add(key, type);
      }
    }

    public void OnBeforeSerialize()
    {
      keys.Clear();
      types.Clear();

      foreach (var kvp in memory)
      {

        string key = kvp.Key;
        string typename = kvp.Value.GetValueType().AssemblyQualifiedName;

        keys.Add(key);
        types.Add(typename);
      }
    }

    #region Register Type Utilities

#if UNITY_EDITOR

    /// <summary>
    /// The types that can be selected from the Inspector.
    /// </summary>       
    public static Type[] registerTypes;
    public static string[] registerTypeNames;

    static Blackboard()
    {
      CollectRegisterTypes();
    }

    private static void CollectRegisterTypes()
    {
      var types = new List<Type>
      {
        typeof(GameObject),
        typeof(Component),
        typeof(Transform),

        typeof(int),
        typeof(bool),
        typeof(float),
        typeof(string),
        typeof(Vector2),
        typeof(Vector3),
        typeof(Quaternion),

        typeof(List<>)
      };

      registerTypes = types.ToArray();
      registerTypeNames = types.Select(t => TypeExtensions.NiceName(t)).ToArray();
    }

    private static readonly Type kUnityObjectType = typeof(UnityEngine.Object);

    public static bool IsUnityObject(Type t)
    {
      return kUnityObjectType.IsAssignableFrom(t);
    }
#endif

    #endregion
  }
}
