
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Bonsai.Core;
using Bonsai.Standard;
using UnityEditor;
using UnityEngine;

namespace Bonsai.Designer
{
  /// <summary>
  /// The editor handles rendering, manipulation and viewing of the canvas.
  /// </summary>
  public class BonsaiEditor
  {
    private readonly BonsaiWindow window;
    public BonsaiCanvas Canvas { get; private set; }
    public Coord Coordinates { get; private set; }

    public EditorSelection NodeSelection { get; } = new EditorSelection();
    public EditorDragging NodeDragging { get; } = new EditorDragging();
    public EditorAreaSelect NodeAreaSelect { get; } = new EditorAreaSelect();
    public EditorNodeLinking NodeLinking { get; } = new EditorNodeLinking();
    public EditorMakingConnection MakingConnection { get; } = new EditorMakingConnection();

    private static Dictionary<Type, NodeTypeProperties> behaviourNodes;

    private BonsaiNode nodeToPositionUnderMouse = null;

    /// <summary>
    /// The multiple that grid snapping rounds to.
    /// It should be a multiple of the grid size.
    /// </summary>
    public static float SnapStep { get { return Preferences.snapStep; } }

    private readonly GUIStyle modeStatusStyle = new GUIStyle { fontSize = 36, fontStyle = FontStyle.Bold };
    private readonly Rect modeStatusRect = new Rect(20f, 20f, 250f, 150f);

    public BonsaiEditor(BonsaiWindow window)
    {
      this.window = window;
      modeStatusStyle.normal.textColor = new Color(1f, 1f, 1f, 0.2f);

      NodeSelection.SingleSelected += OnSingleSelected;
      NodeSelection.AbortSelected += OnAbortSelected;
    }

    private void OnSingleSelected(object sender, BonsaiNode node)
    {
      // Push to end so it is rendered above all other nodes.
      Canvas.PushToEnd(node);

      // This is only required when in Play mode. Force repeaint to see changes immediately.
      if (window.EditorMode == BonsaiWindow.Mode.View)
      {
        window.Repaint();
      }
    }

    private void OnAbortSelected(object sender, ConditionalAbort abort)
    {
      UpdateOrderIndices();
    }

    public void SetBehaviourTree(BehaviourTree tree)
    {
      Canvas = new BonsaiCanvas(tree);
      Coordinates = new Coord(Canvas, window);
    }

    private static BonsaiPreferences Preferences
    {
      get { return BonsaiPreferences.Instance; }
    }

    public void Draw()
    {
      // We need to wait for the generic menu to finish
      // in order to get a valid mouse position.
      HandleNewNodeToPositionUnderMouse();

      if (Event.current.type == EventType.Repaint)
      {
        DrawGrid();
        DrawMode();
      }

      DrawCanvasContents();
    }

    #region Editing

    /// <summary>
    /// Translates the canvas.
    /// </summary>
    /// <param name="delta">The amount to translate the canvas.</param>
    public void Pan(Vector2 delta)
    {
      Canvas.panOffset += delta * Canvas.ZoomScale * BonsaiCanvas.PanSpeed;

      // Round to keep panning sharp.
      Canvas.panOffset.x = Mathf.Round(Canvas.panOffset.x);
      Canvas.panOffset.y = Mathf.Round(Canvas.panOffset.y);
    }

    /// <summary>
    /// Scales the canvas.
    /// </summary>
    /// <param name="zoomDirection">+1 to zoom in and -1 to zoom out.</param>
    public void Zoom(float zoomDirection)
    {
      float scale = (zoomDirection < 0f) ? (1f - BonsaiCanvas.ZoomDelta) : (1f + BonsaiCanvas.ZoomDelta);
      Canvas.zoom *= scale;

      float cap = Mathf.Clamp(Canvas.zoom.x, BonsaiCanvas.MinZoom, BonsaiCanvas.MaxZoom);
      Canvas.zoom.Set(cap, cap);
    }

    public void UpdateNodeGUI(BehaviourNode behaviour)
    {
      BonsaiNode node = Canvas.First(n => n.Behaviour == behaviour);
      node.UpdateGui();
      node.UpdatePortPositions();

      // Snap so connections align.
      node.Center = Coord.SnapPosition(node.Center, SnapStep);
    }

    public void UpdateOrderIndices()
    {
      window.Tree.CalculateTreeOrders();
    }

    #endregion

    #region Drawing

    public void DrawStaticGrid()
    {
      Drawer.DrawStaticGrid(window.CanvasRect, Preferences.gridTexture);
    }

    private void DrawGrid()
    {
      Drawer.DrawGrid(window.CanvasRect, Preferences.gridTexture, Canvas.ZoomScale, Canvas.panOffset);
    }

    private void DrawCanvasContents()
    {
      ScaleUtility.BeginScale(window.CanvasRect, Canvas.ZoomScale, BonsaiWindow.toolbarHeight);

      DrawConnectionPreview();
      DrawPortConnections();
      DrawNodes();

      ScaleUtility.EndScale(window.CanvasRect, Canvas.ZoomScale, BonsaiWindow.toolbarHeight);

      // Selection overlays and independent of zoom.
      DrawAreaSelection();
    }

    private void DrawNodes()
    {
      if (window.EditorMode == BonsaiWindow.Mode.Edit)
      {
        DrawNodesInEditMode();
      }
      else
      {
        DrawNodesInViewMode();
      }
    }

    private void DrawNodesInEditMode()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (Coordinates.IsInView(node))
        {
          Drawer.DrawNode(Coordinates, node, NodeStatusColor(node));
          Drawer.DrawPorts(Coordinates, node);
        }
      }
    }

    // Does not render ports in view mode since nodes cannot be changed.
    private void DrawNodesInViewMode()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (Coordinates.IsInView(node))
        {
          Drawer.DrawNode(Coordinates, node, NodeStatusColor(node));
        }
      }
    }

    private void DrawPortConnections()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (node.Output != null)
        {
          Drawer.DrawDefaultPortConnections(Coordinates, node);
        }
      }
    }

    /// <summary>
    /// Draws the preview connection from the selected output port and the mouse.
    /// </summary>
    private void DrawConnectionPreview()
    {
      // Draw connection between mouse and the port.
      if (MakingConnection.IsMakingConnection)
      {
        var start = Coordinates.CanvasToScreenSpace(MakingConnection.OutputToConnect.RectPosition.center);
        var end = Event.current.mousePosition;
        Drawer.DrawRectConnectionScreenSpace(start, end, Color.white);
        window.Repaint();
      }
    }

    private void DrawAreaSelection()
    {
      if (NodeAreaSelect.IsSelecting)
      {
        // Construct and display the rect.
        Rect selectionRect = NodeAreaSelect.SelectionScreenSpace(Event.current.mousePosition);
        Color selectionColor = new Color(0f, 0.5f, 1f, 0.1f);
        Handles.DrawSolidRectangleWithOutline(selectionRect, selectionColor, Color.blue);

        window.Repaint();
      }
    }

    /// <summary>
    /// Draw the window mode in the background.
    /// </summary>
    public void DrawMode()
    {
      if (!window.Tree)
      {
        GUI.Label(modeStatusRect, new GUIContent("No Tree Set"), modeStatusStyle);
      }

      else if (NodeLinking.IsLinking)
      {
        GUI.Label(modeStatusRect, new GUIContent("Link References"), modeStatusStyle);
      }

      else if (window.EditorMode == BonsaiWindow.Mode.Edit)
      {
        GUI.Label(modeStatusRect, new GUIContent("Edit"), modeStatusStyle);
      }

      else
      {
        GUI.Label(modeStatusRect, new GUIContent("View"), modeStatusStyle);
      }
    }

    #endregion

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
      else if (window.Tree.Root == node.Behaviour)
      {
        return Preferences.rootSymbolColor;
      }

      return Preferences.defaultNodeBackgroundColor;
    }

    [Pure]
    private bool IsNodeRunning(BonsaiNode node)
    {
      return node.Behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running;
    }

    // Highlights nodes that can be aborted by the currently selected node.
    [Pure]
    private bool IsNodeAbortable(BonsaiNode node)
    {
      // Root must exist.
      if (!window.Tree.Root)
      {
        return false;
      }

      BonsaiNode selected = NodeSelection.SelectedNode;

      // A node must be selected.
      if (selected == null)
      {
        return false;
      }

      // The selected node must be a conditional abort.
      var aborter = selected.Behaviour as ConditionalAbort;

      // Node can be aborted by the selected aborter.
      return aborter && ConditionalAbort.IsAbortable(aborter, node.Behaviour);
    }

    /// <summary>
    /// Highlights nodes that are being re-evaluated, like abort nodes.
    /// </summary>
    /// <param name="node"></param>
    [Pure]
    private bool IsNodeEvaluating(BonsaiNode node)
    {
      if (!EditorApplication.isPlaying)
      {
        return false;
      }

      BehaviourIterator itr = node.Behaviour.Iterator;

      if (itr != null && itr.IsRunning)
      {
        var aborter = node.Behaviour as ConditionalAbort;
        int index = itr.CurrentIndex;
        if (index != -1)
        {
          BehaviourNode currentNode = window.Tree.GetNode(index);

          // The current running node can be aborted by it.
          return aborter && ConditionalAbort.IsAbortable(aborter, currentNode);
        }
      }

      return false;
    }

    private void HandleNewNodeToPositionUnderMouse()
    {
      if (nodeToPositionUnderMouse != null)
      {
        nodeToPositionUnderMouse.Position = Coordinates.MousePosition();
        nodeToPositionUnderMouse = null;
      }
    }

    public void SetNewNodeToPositionUnderMouse(BonsaiNode node)
    {
      nodeToPositionUnderMouse = node;
    }

    #region Node Type Properties

    public static void FetchBehaviourNodes()
    {
      behaviourNodes = new Dictionary<Type, NodeTypeProperties>();

      IEnumerable<Type> behaviourTypes = AppDomain.CurrentDomain.GetAssemblies()
         .Where(asm => asm.FullName.Contains("Assembly") && !asm.FullName.Contains("Editor"))
         .SelectMany(asm => asm.GetTypes())
         .Where(t => t.IsSubclassOf(typeof(BehaviourNode)) && !t.IsAbstract);

      foreach (Type type in behaviourTypes)
      {
        var nodeMeta = type.GetCustomAttribute<BonsaiNodeAttribute>(false);

        // Default menu path if unspecified.
        string menuPath = "User/";

        // Service base class is abstract. For Service types, default to Service texture.
        string texName = typeof(Service).IsAssignableFrom(type) ? "Service" : "Play";

        if (nodeMeta != null)
        {
          if (!string.IsNullOrEmpty(nodeMeta.menuPath))
          {
            menuPath = nodeMeta.menuPath;
          }

          // Texxture names are optional. Use only if specified.
          if (!string.IsNullOrEmpty(nodeMeta.texturePath))
          {
            texName = nodeMeta.texturePath;
          }
        }

        bool bCreateInput = false;
        bool bCreateOutput = false;
        bool bCanHaveMultipleChildren = false;

        // Only action nodes have an input and no output.
        if (type.IsSubclassOf(typeof(Task)))
        {
          bCreateInput = true;
        }

        // Composites and decorators have in and out.
        else
        {
          bCreateInput = true;
          bCreateOutput = true;

          // Only composites can have more than 1 child.
          if (type.IsSubclassOf(typeof(Composite)))
          {
            bCanHaveMultipleChildren = true;
          }
        }

        menuPath += type.Name;
        var prop = new NodeTypeProperties(menuPath, texName, bCreateInput, bCreateOutput, bCanHaveMultipleChildren);

        behaviourNodes.Add(type, prop);
      }
    }

    public static IEnumerable<KeyValuePair<Type, NodeTypeProperties>> Behaviours
    {
      get { return behaviourNodes; }
    }

    public static NodeTypeProperties GetNodeTypeProperties(Type t)
    {
      if (behaviourNodes.ContainsKey(t))
      {
        return behaviourNodes[t];
      }

      return null;
    }

    public class NodeTypeProperties
    {
      public string path, texName;
      public bool bCreateInput, bCreateOutput, bCanHaveMultipleChildren;

      public NodeTypeProperties(string path, string texName, bool bCreateInput, bool bCreateOutput, bool bCanHaveMultipleChildren)
      {
        this.path = path;
        this.texName = texName;
        this.bCreateInput = bCreateInput;
        this.bCreateOutput = bCreateOutput;
        this.bCanHaveMultipleChildren = bCanHaveMultipleChildren;
      }
    }

    #endregion
  }
}