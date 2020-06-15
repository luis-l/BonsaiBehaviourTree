

using System;
using Bonsai.Core;
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
      index = Array.FindIndex(Blackboard.registerTypes, t => t == isValueOfTypeNode.type);
      index = Math.Max(0, index);
    }

    protected override void OnBehaviourNodeInspectorGUI()
    {
      var isValueOfTypeNode = target as IsValueOfType;

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Type: ");

      index = EditorGUILayout.Popup(index, Blackboard.registerTypeNames);
      isValueOfTypeNode.type = Blackboard.registerTypes[index];

      EditorGUILayout.EndHorizontal();
    }
  }
}