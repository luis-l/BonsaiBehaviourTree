
using UnityEngine;
using UnityEditor;
using System;

namespace Bonsai.Designer
{
  public class BonsaiViewer
  {
    public static float ZoomDelta { get { return BonsaiPreferences.Instance.zoomDelta; } }
    public static float MinZoom { get { return BonsaiPreferences.Instance.minZoom; } }
    public static float MaxZoom { get { return BonsaiPreferences.Instance.maxZoom; } }
    public static float PanSpeed { get { return BonsaiPreferences.Instance.panSpeed; } }

    public Vector2 zoom = Vector2.one;
    public Vector2 panOffset = Vector2.zero;
    public float ZoomScale { get { return zoom.x; } }

    public BonsaiCanvas Canvas { get; set; }
    public EditorSelection NodeSelection { get; set; }

    private static BonsaiPreferences Preferences
    {
      get { return BonsaiPreferences.Instance; }
    }

    private readonly GUIStyle modeStatusStyle = new GUIStyle { fontSize = 36, fontStyle = FontStyle.Bold };
    private readonly Rect modeStatusRect = new Rect(20f, 20f, 250f, 150f);

    private string editorModeLabel = "No Tree Set";
    private BonsaiEditor.Mode editorMode;

    public Action<CanvasTransform> CustomDraw;
    public Action CustomOverlayDraw;

    public BonsaiViewer()
    {
      modeStatusStyle.normal.textColor = new Color(1f, 1f, 1f, 0.2f);
    }

    public void Draw(CanvasTransform t)
    {
      if (Event.current.type == EventType.Repaint)
      {
        DrawGrid(t);
        DrawMode();
      }

      DrawCanvasContents(t);
    }

    public void DrawStaticGrid(Vector2 size)
    {
      var canvasRect = new Rect(Vector2.zero, size);
      Drawer.DrawStaticGrid(canvasRect, Preferences.gridTexture);
    }

    private void DrawGrid(CanvasTransform t)
    {
      var canvasRect = new Rect(Vector2.zero, t.size);
      Drawer.DrawGrid(canvasRect, Preferences.gridTexture, ZoomScale, panOffset);
    }

    private void DrawCanvasContents(CanvasTransform t)
    {
      var canvasRect = new Rect(Vector2.zero, t.size);
      ScaleUtility.BeginScale(canvasRect, ZoomScale, BonsaiWindow.toolbarHeight);

      CustomDraw?.Invoke(t);
      DrawPortConnections(t);
      DrawNodes(t);

      ScaleUtility.EndScale(canvasRect, ZoomScale, BonsaiWindow.toolbarHeight);

      // Overlays and independent of zoom.
      CustomOverlayDraw?.Invoke();
    }

    private void DrawNodes(CanvasTransform t)
    {
      if (editorMode == BonsaiEditor.Mode.Edit)
      {
        DrawNodesInEditMode(t);
      }
      else
      {
        DrawNodesInViewMode(t);
      }
    }

    private void DrawNodesInEditMode(CanvasTransform t)
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        Drawer.DrawNode(t, node, NodeStatusColor(node));
        Drawer.DrawPorts(t, node);
      }
    }

    // Does not render ports in view mode since nodes cannot be changed.
    private void DrawNodesInViewMode(CanvasTransform t)
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (t.InView(node.RectPositon))
        {
          Drawer.DrawNode(t, node, NodeStatusColor(node));
        }
      }
    }

    private void DrawPortConnections(CanvasTransform t)
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (node.Output != null)
        {
          Drawer.DrawDefaultPortConnections(t, node);
        }
      }
    }

    /// <summary>
    /// Draw the window mode in the background.
    /// </summary>
    public void DrawMode()
    {
      GUI.Label(modeStatusRect, editorModeLabel, modeStatusStyle);
    }

    public void SetEditorMode(BonsaiEditor.Mode status)
    {
      editorMode = status;

      if (Canvas == null)
      {
        editorModeLabel = "No Tree Set";
      }
      else
      {
        editorModeLabel = status == BonsaiEditor.Mode.Edit ? "Edit" : "View";
      }
    }

    private Color NodeStatusColor(BonsaiNode node)
    {
      if (IsNodeEvaluating(node))
      {
        return Preferences.evaluateColor;
      }
      else if (IsNodeRunning(node))
      {
        return Preferences.runningColor;
      }
      else if (NodeSelection.IsNodeSelected(node))
      {
        return Preferences.selectedColor;
      }
      else if (NodeSelection.IsReferenced(node))
      {
        return Preferences.referenceColor;
      }
      else if (IsNodeAbortable(node))
      {
        return Preferences.abortColor;
      }
      else if (Canvas.Tree.Root == node.Behaviour)
      {
        return Preferences.rootSymbolColor;
      }

      return Preferences.defaultNodeBackgroundColor;
    }

    private bool IsNodeRunning(BonsaiNode node)
    {
      return node.Behaviour.GetStatusEditor() == Core.BehaviourNode.StatusEditor.Running;
    }

    // Highlights nodes that can be aborted by the currently selected node.
    private bool IsNodeAbortable(BonsaiNode node)
    {
      // Root must exist.
      if (!Canvas.Tree.Root)
      {
        return false;
      }

      BonsaiNode selected = NodeSelection.SingleSelectedNode;

      // A node must be selected.
      if (selected == null)
      {
        return false;
      }

      // The selected node must be a conditional abort.
      var aborter = selected.Behaviour as Core.ConditionalAbort;

      // Node can be aborted by the selected aborter.
      return aborter && Core.ConditionalAbort.IsAbortable(aborter, node.Behaviour);
    }

    /// <summary>
    /// Highlights nodes that are being re-evaluated, like abort nodes.
    /// </summary>
    /// <param name="node"></param>
    private bool IsNodeEvaluating(BonsaiNode node)
    {
      if (!EditorApplication.isPlaying)
      {
        return false;
      }

      Core.BehaviourIterator itr = node.Behaviour.Iterator;

      if (itr != null && itr.IsRunning)
      {
        var aborter = node.Behaviour as Core.ConditionalAbort;
        int index = itr.CurrentIndex;
        if (index != -1)
        {
          Core.BehaviourNode currentNode = Canvas.Tree.GetNode(index);

          // The current running node can be aborted by it.
          return aborter && Core.ConditionalAbort.IsAbortable(aborter, currentNode);
        }
      }

      return false;
    }
  }
}
