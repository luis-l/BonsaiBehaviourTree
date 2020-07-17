
using System;
using System.Text;
using Bonsai.Core;
using Bonsai.Designer;
using UnityEngine;

namespace Bonsai.Standard
{
  /// <summary>
  /// Tests if the value at the given key is a certain type.
  /// </summary>
  [BonsaiNode("Conditional/", "Condition")]
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
      if (type == null || !Blackboard.Contains(key))
      {
        return false;
      }

      object value = Blackboard.Get(key);

      // Value is unset, nothing to check.
      if (value == null)
      {
        return false;
      }

      Type valueType = value.GetType();

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

    public override void Description(StringBuilder builder)
    {
      base.Description(builder);
      builder.AppendLine();

      if (key == null || key.Length == 0)
      {
        builder.Append("No key is set to check");
      }
      else
      {
        builder.AppendFormat("Blackboard key {0} is {1}", key, Utility.TypeExtensions.NiceName(type));
      }
    }
  }
}