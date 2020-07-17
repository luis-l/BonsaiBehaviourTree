

using System;
using System.Collections.Generic;
using System.Linq;
using Bonsai.Standard;
using UnityEditor;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(IsValueOfType))]
  public class IsValueOfTypeInspector : BehaviourNodeInspector
  {
    int index = 0;

    protected override void OnEnable()
    {
      base.OnEnable();
      var isValueOfTypeNode = target as IsValueOfType;
      index = Array.FindIndex(registerTypes, t => t == isValueOfTypeNode.type);
      index = Math.Max(0, index);
    }

    protected override void OnBehaviourNodeInspectorGUI()
    {
      var isValueOfTypeNode = target as IsValueOfType;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Type: ");

      index = EditorGUILayout.Popup(index, registerTypeNames);
      isValueOfTypeNode.type = registerTypes[index];

      EditorGUILayout.EndHorizontal();
    }

#if UNITY_EDITOR

    /// <summary>
    /// The types that can be selected from the Inspector.
    /// </summary>       
    public static Type[] registerTypes;
    public static string[] registerTypeNames;

    static IsValueOfTypeInspector()
    {
      CollectRegisterTypes();
    }

    private static void CollectRegisterTypes()
    {
      var types = new List<Type>
      {
        typeof(UnityEngine.GameObject),
        typeof(UnityEngine.Component),
        typeof(UnityEngine.Transform),

        typeof(int),
        typeof(bool),
        typeof(float),
        typeof(string),
        typeof(UnityEngine.Vector2),
        typeof(UnityEngine.Vector3),
        typeof(UnityEngine.Quaternion),

        typeof(List<>)
      };

      registerTypes = types.ToArray();
      registerTypeNames = types.Select(t => Utility.TypeExtensions.NiceName(t)).ToArray();
    }

#endif
  }
}