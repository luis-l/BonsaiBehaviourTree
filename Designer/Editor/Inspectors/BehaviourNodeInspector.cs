
using System.Linq;
using Bonsai.Core;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// All behaviour tree nodes will use this inspector so GUI changes are reflected immediately in the tree editor.
  /// </summary>
  [CustomEditor(typeof(BehaviourNode), true)]
  public class BehaviourNodeInspector : Editor
  {
    // The bonsai window associated with the target's inspector behaviour.
    protected BonsaiWindow ParentWindow { get; private set; }

    protected virtual void OnEnable()
    {
      var edited = target as BehaviourNode;

      // Find the the editor window with the tree associated with this behaviour.
      ParentWindow = Resources.FindObjectsOfTypeAll<BonsaiWindow>().First(w => w.Tree == edited.Tree);
    }

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();
      OnBehaviourNodeInspectorGUI();

      // If the behaviour was edited, update the tree editor and repaint.
      if (GUI.changed)
      {
        ParentWindow.BehaviourNodeEdited(target as BehaviourNode);
      }
    }

    /// <summary>
    /// Child editors will override to draw the inspector.
    /// </summary>
    protected virtual void OnBehaviourNodeInspectorGUI() { }
  }
}
