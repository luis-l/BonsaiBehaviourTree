
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(BehaviourTree))]
  public class BonsaiTreeInspector : Editor
  {
    private string keyNameSelection = "";

    // State of the fold out if it is opened or closed.
    private bool showBlackboardFoldout = true;

    // Cache the label spacing options.
    private static readonly GUILayoutOption[] deleteButtonSize = new GUILayoutOption[] { GUILayout.Width(18f) };

    void OnEnable()
    {
      if (EditorApplication.isPlaying)
      {
        EditorApplication.update += Repaint;
      }
    }

    void OnDisable()
    {
      EditorApplication.update -= Repaint;
    }

    public override void OnInspectorGUI()
    {
      var tree = target as BehaviourTree;
      var bb = tree.blackboard;

      EditorGUILayout.LabelField("Behaviour Tree", tree.name);
      EditorGUILayout.Space();

      if (bb)
      {
        showBlackboardFoldout = EditorGUILayout.Foldout(showBlackboardFoldout, "Blackboard", true);

        if (showBlackboardFoldout)
        {
          EditorGUILayout.Space();
          EditorGUILayout.BeginVertical();
          ShowBlackboardGUI(bb);
          EditorGUILayout.EndVertical();
        }
      }
      else
      {
        EditorGUILayout.LabelField("Blackboard", "Unset");
      }

      EditorGUILayout.Space();
      ShowTreeStats(tree);
    }

    private void ShowTreeStats(BehaviourTree tree)
    {
      if (EditorApplication.isPlaying && tree.IsInitialized())
      {
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Total nodes", tree.Nodes.Length.ToString());
        EditorGUILayout.LabelField("Active timers", tree.ActiveTimerCount.ToString());
        EditorGUILayout.LabelField("Observer count", tree.blackboard ? tree.blackboard.ObserverCount.ToString() : "0");
      }
    }

    private void ShowBlackboardGUI(Blackboard bb)
    {
      KeyAddGUI(bb);

      EditorGUILayout.Space();

      if (bb.Count == 0)
      {
        EditorGUILayout.LabelField("Blackboard is empty");
        return;
      }

      string keyToRemove = null;


      foreach (var kvp in bb.Memory)
      {
        EditorGUILayout.BeginHorizontal();

        string key = kvp.Key;
        object valueObj = kvp.Value;
        string value = valueObj == null ? "Unset" : valueObj.ToString();

        EditorGUILayout.LabelField(key, value);

        if (GUILayout.Button("X", deleteButtonSize))
        {
          keyToRemove = key;
        }

        EditorGUILayout.EndHorizontal();
      }

      // Handle key removal if requested.
      if (keyToRemove != null)
      {
        bb.Remove(keyToRemove);
      }

      ClearAllKeysGUI(bb);
    }

    // GUI Interface to add a new key to the Blackboard.
    private void KeyAddGUI(Blackboard bb)
    {
      EditorGUILayout.BeginHorizontal();

      keyNameSelection = EditorGUILayout.TextField(keyNameSelection);

      if (GUILayout.Button("Add Key"))
      {
        if (!string.IsNullOrEmpty(keyNameSelection))
        {
          bb.Set(keyNameSelection);
        }
      }

      EditorGUILayout.EndHorizontal();
    }

    private void ClearAllKeysGUI(Blackboard bb)
    {
      if (bb.Count > 1 && GUILayout.Button("Clear All"))
      {
        bool isYes = EditorUtility.DisplayDialog(
          "Clear Blackboard",
          "Are you sure you want to delete all keys?",
          "Yes",
          "Cancel");

        if (isYes)
        {
          bb.Clear();
        }
      }
    }
  }
}