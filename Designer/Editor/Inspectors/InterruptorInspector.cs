
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
        message = "Link Interruptables";
      }

      if (GUILayout.Button(message))
      {

        // Toggle
        isLinking = !isLinking;

        if (isLinking)
        {

          ParentWindow.Editor.NodeLinking.BeginLinking(typeof(Interruptable), OnNodeSelectedForLinking);
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
      ParentWindow.Editor.NodeSelection.SetReferenced(interruptor);
    }
  }
}