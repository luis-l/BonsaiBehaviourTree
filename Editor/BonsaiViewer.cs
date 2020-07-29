
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// The current nodes that can be aborted from the currently selected ConditionalAbort.
    /// </summary>
    private HashSet<BonsaiNode> abortableSelected = new HashSet<BonsaiNode>();

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
      DrawConnections(t);
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
      var nodes = Canvas.Nodes;
      for (int i = 0; i < nodes.Count; i++)
      {
        BonsaiNode node = nodes[i];
        Drawer.DrawNode(t, node, NodeStatusColor(node));
        Drawer.DrawPorts(t, node);
      }
    }

    // Does not render ports in view mode since nodes cannot be changed.
    private void DrawNodesInViewMode(CanvasTransform t)
    {
      var nodes = Canvas.Nodes;
      for (int i = 0; i < nodes.Count; i++)
      {
        BonsaiNode node = nodes[i];
        if (t.InView(node.RectPositon))
        {
          Drawer.DrawNode(t, node, NodeStatusColor(node));
        }
      }
    }

    private void DrawConnections(CanvasTransform t)
    {
      var nodes = Canvas.Nodes;
      for (int i = 0; i < nodes.Count; i++)
      {
        BonsaiNode node = nodes[i];

        if (node.HasOutput)
        {
          Drawer.DrawNodeConnections(t, node);
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

      if (Canvas == null || Canvas.Tree == null)
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
      if (IsNodeObserving(node))
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
      else if (abortableSelected.Contains(node))
      {
        return Preferences.abortColor;
      }
      else if (Canvas.Root == node)
      {
        return Preferences.rootSymbolColor;
      }

      return Preferences.defaultNodeBackgroundColor;
    }

    private bool IsNodeRunning(BonsaiNode node)
    {
      return node.Behaviour.StatusEditorResult == Core.BehaviourNode.StatusEditor.Running;
    }

    /// <summary>
    /// Highlights nodes that are being re-evaluated, like abort nodes.
    /// </summary>
    /// <param name="node"></param>
    private bool IsNodeObserving(BonsaiNode node)
    {
      Core.BehaviourNode behaviour = node.Behaviour;
      Core.BehaviourIterator itr = behaviour.Iterator;

      if (itr != null && itr.IsRunning)
      {
        var aborter = behaviour as Core.ConditionalAbort;
        return aborter && aborter.IsObserving;
      }

      return false;
    }

    public void UpdateAbortableSelection(BonsaiNode node)
    {
      abortableSelected.Clear();

      var aborter = node.Behaviour as Core.ConditionalAbort;
      if (aborter)
      {
        abortableSelected = new HashSet<BonsaiNode>(Abortables(node, aborter.abortType));
      }
    }

    public void ClearAbortableSelection()
    {
      abortableSelected.Clear();
    }

    private IEnumerable<BonsaiNode> Abortables(BonsaiNode aborter, Core.AbortType abortType)
    {
      switch (abortType)
      {
        case Core.AbortType.Self:
          return SelfAbortables(aborter);
        case Core.AbortType.LowerPriority:
          return LowerPriorityAbortables(aborter);
        case Core.AbortType.Both:
          return SelfAbortables(aborter).Concat(LowerPriorityAbortables(aborter));
        default:
          return Enumerable.Empty<BonsaiNode>();
      }
    }

    private IEnumerable<BonsaiNode> SelfAbortables(BonsaiNode aborter)
    {
      return Core.TreeTraversal.PreOrder(aborter).Skip(1);
    }

    private IEnumerable<BonsaiNode> LowerPriorityAbortables(BonsaiNode aborter)
    {
      GetCompositeParent(aborter, out BonsaiNode parent, out BonsaiNode directChild);
      if (parent != null)
      {
        parent.SortChildren();
        int abortIndex = parent.IndexOf(directChild);
        if (abortIndex >= 0)
        {
          return Enumerable
            .Range(0, parent.ChildCount())
            .Where(i => i > abortIndex)
            .SelectMany(i => Core.TreeTraversal.PreOrder(parent.GetChildAt(i)));
        }
      }
      return Enumerable.Empty<BonsaiNode>();
    }

    private static void GetCompositeParent(BonsaiNode aborter, out BonsaiNode compositeParent, out BonsaiNode directChild)
    {
      directChild = aborter;
      compositeParent = aborter.Parent;
      while (compositeParent != null && !compositeParent.Behaviour.IsComposite())
      {
        directChild = compositeParent;
        compositeParent = compositeParent.Parent;
      }
    }
  }
}
