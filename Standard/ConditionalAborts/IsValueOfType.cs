
using System;
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  /// <summary>
  /// Tests if the value at the given key is a certain type.
  /// </summary>
  [NodeEditorProperties("Conditional/", "Condition")]
  public class IsValueOfType : ConditionalAbort, ISerializationCallbackReceiver
  {
    [Tooltip("The key of the value to test its type.")]
    public string key;

    /// <summary>
    /// The type to test against.
    /// </summary>
    public Type type;

    // Since Unity cannot serialize Type, we need to store the full name of the type.
    [SerializeField, HideInInspector]
    private string _typename;

    [Tooltip("Use Type.IsAssignableFrom() to take into account inheritance.")]
    public bool useIsAssignableFrom = false;

    public override bool Condition()
    {
      if (!Blackboard.Exists(key))
      {
        return false;
      }

      Type valueType = Blackboard.GetRegister(key).GetValueType();

      return useIsAssignableFrom ? type.IsAssignableFrom(valueType) : valueType == type;
    }

    public void OnAfterDeserialize()
    {
      if (string.IsNullOrEmpty(_typename))
      {
        type = null;
      }

      else
      {
        type = Type.GetType(_typename);
      }
    }

    public void OnBeforeSerialize()
    {
      if (type != null)
      {
        _typename = type.AssemblyQualifiedName;
      }

      else
      {
        _typename = "";
      }
    }
  }
}