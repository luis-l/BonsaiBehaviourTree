
using Bonsai.Core;
using Bonsai.Standard;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(Guard))]
  public class GuardInspector : BehaviourNodeInspector
  {
    private bool isLinking = false;

    private Guard guard;

    protected override void OnEnable()
    {
      base.OnEnable();
      guard = target as Guard;
    }

    void OnDestroy()
    {
      ParentWindow.Editor.NodeLinking.EndLinking();
    }

    protected override void OnBehaviourNodeInspectorGUI()
    {
      EditorGUILayout.BeginVertical();

      string message;

      if (isLinking)
      {
        message = "Finish Linking";
      }

      else
      {
        message = "Link Guards";
      }

      if (GUILayout.Button(message))
      {

        // Toggle
        isLinking = !isLinking;

        if (isLinking)
        {
          ParentWindow.Editor.NodeLinking.BeginLinking(typeof(Guard), OnNodeSelectedForLinking);
          ParentWindow.Repaint();
        }

        else
        {
          ParentWindow.Editor.NodeLinking.EndLinking();
          ParentWindow.Repaint();
        }
      }

      if (ParentWindow)
      {
        isLinking = ParentWindow.Editor.NodeLinking.IsLinking;
      }

      EditorGUILayout.EndVertical();

      if (GUI.changed)
      {

        // Synchronize the key values.
        foreach (Guard linkedGuard in guard.linkedGuards)
        {
          linkedGuard.maxActiveGuards = guard.maxActiveGuards;
          linkedGuard.waitUntilChildAvailable = guard.waitUntilChildAvailable;
          linkedGuard.returnSuccessOnSkip = guard.returnSuccessOnSkip;
        }
      }
    }

    private void OnNodeSelectedForLinking(BehaviourNode node)
    {
      // Cannot link itself.
      if (node == guard)
      {
        return;
      }

      serializedObject.Update();

      var refGuard = node as Guard;
      bool bAlreadyLinked = guard.linkedGuards.Contains(refGuard);

      // Works as a toggle, if already linked then unlink.
      if (bAlreadyLinked)
      {
        guard.linkedGuards.Remove(refGuard);

        // The rest of the guards forget about this unlinked guard too.
        foreach (Guard linkedGuard in guard.linkedGuards)
        {
          linkedGuard.linkedGuards.Remove(refGuard);
        }

        // The unlinked guard also loses all its linked guards.
        refGuard.linkedGuards.Clear();
      }

      // If unlinked, then link.
      else
      {
        // The other guards must reference the new linked guard too.
        foreach (Guard link in guard.linkedGuards)
        {
          link.linkedGuards.Add(refGuard);

          // The new linked guard must know about the other already linked guards.
          refGuard.linkedGuards.Add(link);
        }

        // This guard add the new linked guard.
        guard.linkedGuards.Add(refGuard);

        // The new linked guard must also know about this guard.
        refGuard.linkedGuards.Add(guard);
      }

      serializedObject.ApplyModifiedProperties();

      // Update the referenced nodes in the editor.
      ParentWindow.Editor.NodeSelection.SetReferenced(guard);
    }
  }
}