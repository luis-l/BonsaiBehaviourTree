
using UnityEngine;
using UnityEditor;

using Bonsai.Core;
using Bonsai.Standard;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(Guard))]
  public class GuardInspector : Editor
  {
    private bool _bIsLinking = false;

    // The bonsai window associated with the target's inspector behaviour.
    private BonsaiWindow parentWindow = null;

    private Guard _guard;

    void OnEnable()
    {
      _guard = target as Guard;
      BehaviourTree bt = _guard.Tree;

      var editorWindows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();

      // Find the the editor window with the tree associated with this behaviour.
      foreach (BonsaiWindow win in editorWindows)
      {

        // Found the tree, cache this window.
        if (win.Tree == bt)
        {
          parentWindow = win;
          break;
        }
      }
    }

    void OnDestroy()
    {
      // Make sure to cleanup.
      parentWindow.InputHandler.EndReferenceLinking();
    }

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      EditorGUILayout.BeginVertical();

      string message;

      if (_bIsLinking)
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
        _bIsLinking = !_bIsLinking;

        if (_bIsLinking)
        {

          parentWindow.InputHandler.StartReferenceLinking(typeof(Guard), onNodeSelectedForLinking);
          parentWindow.Repaint();
        }

        else
        {

          parentWindow.InputHandler.EndReferenceLinking();
          parentWindow.Repaint();
        }
      }

      if (parentWindow)
      {
        _bIsLinking = parentWindow.InputHandler.IsRefLinking;
      }

      EditorGUILayout.EndVertical();

      if (GUI.changed)
      {

        // Synchronize the key values.
        foreach (Guard linkedGuard in _guard.linkedGuards)
        {
          linkedGuard.maxActiveGuards = _guard.maxActiveGuards;
          linkedGuard.waitUntilChildAvailable = _guard.waitUntilChildAvailable;
          linkedGuard.returnSuccessOnSkip = _guard.returnSuccessOnSkip;
        }
      }
    }

    private void onNodeSelectedForLinking(BehaviourNode node)
    {
      // Cannot link itself.
      if (node == _guard)
      {
        return;
      }

      serializedObject.Update();

      var refGuard = node as Guard;
      bool bAlreadyLinked = _guard.linkedGuards.Contains(refGuard);

      // Works as a toggle, if already linked then unlink.
      if (bAlreadyLinked)
      {

        _guard.linkedGuards.Remove(refGuard);

        // The rest of the guards forget about this unlinked guard too.
        foreach (Guard linkedGuard in _guard.linkedGuards)
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
        foreach (Guard link in _guard.linkedGuards)
        {
          link.linkedGuards.Add(refGuard);

          // The new linked guard must know about the other already linked guards.
          refGuard.linkedGuards.Add(link);
        }

        // This guard add the new linked guard.
        _guard.linkedGuards.Add(refGuard);

        // The new linked guard must also know about this guard.
        refGuard.linkedGuards.Add(_guard);
      }

      serializedObject.ApplyModifiedProperties();

      // Update the referenced nodes in the editor.
      var refs = _guard.GetReferencedNodes();
      parentWindow.Editor.SetReferencedNodes(refs);
    }
  }
}