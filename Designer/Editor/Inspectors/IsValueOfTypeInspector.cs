
using System;

using UnityEngine;
using UnityEditor;

using Bonsai.Standard;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(IsValueOfType))]
  public class IsValueOfTypeInspector : Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var isValueOfTypeNode = target as IsValueOfType;

      Type typeToCheck = isValueOfTypeNode.type;

      string typename = "null";

      if (typeToCheck != null)
      {
        typename = isValueOfTypeNode.type.Name;
      }

      float size = new GUIStyle().CalcSize(new GUIContent(typename)).x + 20f;
      var opt = new GUILayoutOption[] { GUILayout.Width(size) };

      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Type: ");

      // Display the type selection menu.
      if (EditorGUILayout.DropdownButton(new GUIContent(typename), FocusType.Keyboard, opt))
      {

        // var selectTypeTab = new Tab<Type>(

        //     getValues: () => Blackboard.registerTypes,
        //     getCurrent: () => isValueOfTypeNode.type,
        //     setTarget: (t) => { isValueOfTypeNode.type = t; },
        //     getValueName: (t) => t.GetNiceName(),
        //     title: "Select Type"
        // );

        // SelectionWindow.Show(selectTypeTab);
      }

      EditorGUILayout.EndHorizontal();
    }
  }
}