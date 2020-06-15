
using Bonsai.Core;
using Bonsai.Standard;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  [CustomEditor(typeof(Interruptor))]
  public class InterruptorInspector : BehaviourNodeInspector
  {
    private bool isLinking = false;

    private Interruptor interruptor;

    protected override void OnEnable()
    {
      base.OnEnable();
      interruptor = target as Interruptor;
    }

    void OnDestroy()
    {
      // Make sure to cleanup.
      ParentWindow.InputHandler.EndReferenceLinking();
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
        message = "Link Interruptables";
      }

      if (GUILayout.Button(message))
      {

        // Toggle
        isLinking = !isLinking;

        if (isLinking)
        {

          ParentWindow.InputHandler.StartReferenceLinking(typeof(Interruptable), OnNodeSelectedForLinking);
          ParentWindow.Repaint();
        }

        else
        {

          ParentWindow.InputHandler.EndReferenceLinking();
          ParentWindow.Repaint();
        }
      }

      if (ParentWindow)
      {
        isLinking = ParentWindow.InputHandler.IsRefLinking;
      }

      EditorGUILayout.EndVertical();
    }

    private void OnNodeSelectedForLinking(BehaviourNode node)
    {
      serializedObject.Update();

      var refInter = node as Interruptable;
      bool bAlreadyLinked = interruptor.linkedInterruptables.Contains(refInter);

      // Works as a toggle, if already linked then unlink.
      if (bAlreadyLinked)
      {
        interruptor.linkedInterruptables.Remove(refInter);

      }

      // If unlinked, then link.
      else
      {
        interruptor.linkedInterruptables.Add(refInter);
      }

      serializedObject.ApplyModifiedProperties();

      // Update the referenced nodes in the editor.
      var refs = interruptor.GetReferencedNodes();
      ParentWindow.Editor.SetReferencedNodes(refs);
    }
  }
}