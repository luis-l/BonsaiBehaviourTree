using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Bonsai.Core;
using Bonsai.Standard;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(Interruptor))]
  public class InterruptorInspector : Editor
  {
    private bool _bIsLinking = false;

    // The bonsai window associated with the target's inspector behaviour.
    private BonsaiWindow parentWindow = null;

    private Interruptor _interruptor;

    void OnEnable()
    {
      _interruptor = target as Interruptor;
      BehaviourTree bt = _interruptor.Tree;

      var editorWindows = Resources.FindObjectsOfTypeAll<BonsaiWindow>();

      // Find the the editor window with the tree associated with this behaviour.
      foreach (BonsaiWindow win in editorWindows)
      {

        // Found the tree, cache this window.
        if (win.tree == bt)
        {
          parentWindow = win;
          break;
        }
      }
    }

    void OnDestroy()
    {
      // Make sure to cleanup.
      parentWindow.inputHandler.EndReferenceLinking();
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
        message = "Link Interruptables";
      }

      if (GUILayout.Button(message))
      {

        // Toggle
        _bIsLinking = !_bIsLinking;

        if (_bIsLinking)
        {

          parentWindow.inputHandler.StartReferenceLinking(typeof(Interruptable), onNodeSelectedForLinking);
          parentWindow.Repaint();
        }

        else
        {

          parentWindow.inputHandler.EndReferenceLinking();
          parentWindow.Repaint();
        }
      }

      if (parentWindow)
      {
        _bIsLinking = parentWindow.inputHandler.IsRefLinking;
      }

      EditorGUILayout.EndVertical();
    }

    private void onNodeSelectedForLinking(BehaviourNode node)
    {
      serializedObject.Update();

      var refInter = node as Interruptable;
      bool bAlreadyLinked = _interruptor.linkedInterruptables.Contains(refInter);

      // Works as a toggle, if already linked then unlink.
      if (bAlreadyLinked)
      {
        _interruptor.linkedInterruptables.Remove(refInter);

      }

      // If unlinked, then link.
      else
      {
        _interruptor.linkedInterruptables.Add(refInter);
      }

      serializedObject.ApplyModifiedProperties();

      // Update the referenced nodes in the editor.
      var refs = _interruptor.GetReferencedNodes();
      parentWindow.editor.SetReferencedNodes(refs);
    }
  }
}