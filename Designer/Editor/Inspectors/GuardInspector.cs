using Bonsai.Standard;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(Guard))]
  public class GuardInspector : BehaviourNodeInspector
  {
    private Guard guard;

    private readonly GUIStyle linkStyle = new GUIStyle();

    protected override void OnEnable()
    {
      base.OnEnable();
      guard = target as Guard;
      linkStyle.alignment = TextAnchor.MiddleCenter;
      linkStyle.fontStyle = FontStyle.Bold;
    }

    protected override void OnBehaviourNodeInspectorGUI()
    {
      EditorGUILayout.BeginVertical();
      EditorGUILayout.LabelField("Guards Linked", guard.linkedGuards.Count.ToString());
      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Shift + Click to Link Guards", linkStyle);
      EditorGUILayout.EndVertical();

      guard.maxActiveGuards = Mathf.Min(guard.maxActiveGuards, guard.linkedGuards.Count);

      if (GUI.changed)
      {
        foreach (Guard other in guard.linkedGuards)
        {
          other.maxActiveGuards = guard.maxActiveGuards;
          ParentWindow.UpdateNodeGUI(other);
        }
      }
    }
  }
}