using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// All behaviour tree nodes will use this inspector so GUI changes are reflected immediately in the tree editor.
  /// </summary>
  [CanEditMultipleObjects]
  [CustomEditor(typeof(BehaviourNode), true)]
  public class BehaviourNodeInspector : Editor
  {
    // The bonsai window associated with the target's inspector behaviour.
    protected BonsaiWindow ParentWindow { get; private set; }

    private readonly GUIContent descriptionHeader = new GUIContent("Node Description");
    private GUIContent runtimeHeading;

    // BehaviourNode fields to show when the tree is running in Play mode. e.g. The time left on a timer.
    private Dictionary<string, FieldInfo> runtimeFields = new Dictionary<string, FieldInfo>();

    SerializedProperty nodeTitle;
    SerializedProperty nodeComment;

    protected virtual void OnEnable()
    {
      var edited = target as BehaviourNode;

      nodeTitle = serializedObject.FindProperty("title");
      nodeComment = serializedObject.FindProperty("comment");

      // Find the the editor window with the tree associated with this behaviour.
      if (ParentWindow == null)
      {
        ParentWindow = Resources.FindObjectsOfTypeAll<BonsaiWindow>().First(w => w.ContainsNode(edited));

        if (ParentWindow.EditorMode == BonsaiEditor.Mode.View)
        {
          runtimeHeading = new GUIContent("Runtime values");
          runtimeFields = GetRuntimeFields(edited);
        }
      }
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();
      DrawDefaultInspector();
      OnBehaviourNodeInspectorGUI();
      DrawNodeDescription();

      // If the behaviour was edited, update the tree editor and repaint.
      if (GUI.changed)
      {
        serializedObject.ApplyModifiedProperties();
        ParentWindow.UpdateSelectedNodesGUI();
      }

      if (ParentWindow.EditorMode == BonsaiEditor.Mode.View)
      {
        DrawRuntimeValues();
      }
    }

    /// <summary>
    /// Child editors will override to draw the inspector.
    /// </summary>
    protected virtual void OnBehaviourNodeInspectorGUI() { }

    public override bool RequiresConstantRepaint()
    {
      // Repaint to see runtime values changes when tree runs.
      return ParentWindow.EditorMode == BonsaiEditor.Mode.View && runtimeFields.Count != 0;
    }

    private void DrawNodeDescription()
    {
      EditorGUILayout.Space();
      EditorGUILayout.LabelField(descriptionHeader, EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(nodeTitle);
      EditorGUILayout.PropertyField(nodeComment);
    }

    private void DrawRuntimeValues()
    {
      if (runtimeFields.Count != 0)
      {
        EditorGUILayout.Space();
        GUILayout.Label(runtimeHeading, EditorStyles.boldLabel);

        foreach (var fields in runtimeFields)
        {
          EditorGUILayout.LabelField(fields.Key, fields.Value.GetValue(target).ToString());
        }
      }
    }

    private Dictionary<string, FieldInfo> GetRuntimeFields(BehaviourNode target)
    {
      return target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(f => f.GetCustomAttribute<ShowAtRuntimeAttribute>() != null)
        .ToDictionary(f => RuntimeFieldLabel(f), f => f);
    }

    private static string RuntimeFieldLabel(FieldInfo f)
    {
      var view = f.GetCustomAttribute<ShowAtRuntimeAttribute>();
      return ObjectNames.NicifyVariableName(string.IsNullOrEmpty(view.label) ? f.Name : view.label);
    }
  }
}
