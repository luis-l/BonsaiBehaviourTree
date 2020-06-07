

using System;

using UnityEditor;

using Bonsai.Standard;
using Bonsai.Core;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(IsValueOfType))]
  public class IsValueOfTypeInspector : Editor
  {
    int index = 0;

    void OnEnable()
    {
      var isValueOfTypeNode = target as IsValueOfType;
      index = Array.FindIndex(Blackboard.registerTypes, t => t == isValueOfTypeNode.type);
    }

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var isValueOfTypeNode = target as IsValueOfType;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Type: ");

      index = EditorGUILayout.Popup(index, Blackboard.registerTypeNames);
      isValueOfTypeNode.type = Blackboard.registerTypes[index];

      EditorGUILayout.EndHorizontal();
    }
  }
}