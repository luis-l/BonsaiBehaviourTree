
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

    private static Dictionary<Type, NodeTypeProperties> behaviourNodes;

    private BonsaiNode nodeToPositionUnderMouse = null;

    // Remembers which nodes are currently being referenced by another node.
    private readonly HashSet<BehaviourNode> referencedNodes = new HashSet<BehaviourNode>();

    // The types of node which can store refs to other nodes.
    public readonly HashSet<Type> referenceContainerTypes = new HashSet<Type>();

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
      referenceContainerTypes.Add(typeof(Interruptor));
      referenceContainerTypes.Add(typeof(Guard));
      modeStatusStyle.normal.textColor = new Color(1f, 1f, 1f, 0.2f);
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

    /// <summary>
    /// Sets the position of the subtree at an offset.
    /// </summary>
    /// <param name="pos">The position of the subtree. </param>
    /// <param name="offset">Additional offset.</param>
    /// <param name="root">The subtree root.</param>
    public void SetSubtreePosition(Vector2 pos, Vector2 offset, BonsaiNode root)
    {
      float min = float.MinValue;

      if (root.Input.outputConnection != null)
      {

        float nodeTop = root.Input.RectPosition.yMin;
        float parentBottom = root.Input.outputConnection.RectPosition.yMax;

        // The root cannot be above its parent.
        if (nodeTop < parentBottom)
        {
          min = parentBottom;
        }
      }

      // Record the old position so we can know by how much the root moved
      // so all children can be shifted by the pan delta.
      Vector2 oldPos = root.Center;

      // Clamp the position so it does not go above the parent.
      Vector2 diff = pos - offset;
      diff.y = Mathf.Clamp(diff.y, min, float.MaxValue);

      Vector2 rounded = Coord.SnapPosition(diff, SnapStep);
      root.Center = rounded;

      // Calculate the change of position of the root.
      Vector2 pan = root.Center - oldPos;

      // Move the entire subtree of the root.
      TreeIterator<BonsaiNode>.Traverse(root, node =>
      {
        // For all children, pan by the same amount that the parent changed by.
        if (node != root)
          node.Center += Coord.SnapPosition(pan, SnapStep);
      });
    }

    public void UpdateOrderIndices()
    {
      window.Tree.CalculateTreeOrders();
    }

    public void SetReferencedNodes(IEnumerable<BehaviourNode> nodes)
    {
      if (nodes == null)
      {
        return;
      }

      referencedNodes.Clear();
      foreach (BehaviourNode node in nodes)
      {
        referencedNodes.Add(node);
      }
    }

    public void ClearReferencedNodes()
    {
      referencedNodes.Clear();
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
      if (window.InputHandler.IsMakingConnection)
      {
        var start = Coordinates.CanvasToScreenSpace(window.InputHandler.OutputToConnect.RectPosition.center);
        var end = Event.current.mousePosition;
        Drawer.DrawRectConnectionScreenSpace(start, end, Color.white);
        window.Repaint();
      }
    }

    private void DrawAreaSelection()
    {
      if (window.InputHandler.IsAreaSelecting)
      {
        // Construct and display the rect.
        Rect selectionRect = window.InputHandler.SelectionScreenSpace();
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

      else if (window.InputHandler.IsRefLinking)
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
      else if (IsNodeSelected(node))
      {
        return Preferences.selectedColor;
      }
      else if (IsNodeReferenced(node))
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

      BonsaiNode selected = window.InputHandler.SelectedNode;

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

    // Nodes that are being referenced are highlighted.
    [Pure]
    private bool IsNodeReferenced(BonsaiNode node)
    {
      return referencedNodes.Contains(node.Behaviour);
    }

    [Pure]
    private bool IsNodeSelected(BonsaiNode node)
    {
      return node.Behaviour == Selection.activeObject || node.bAreaSelectionFlag;
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

      IEnumerable<Assembly> scriptAssemblies = AppDomain.CurrentDomain.GetAssemblies().
          Where((Assembly assembly) => assembly.FullName.Contains("Assembly")
              && !assembly.FullName.Contains("Editor"));

      Type targetType = typeof(BehaviourNode);

      foreach (Assembly assembly in scriptAssemblies)
      {

        foreach (Type type in assembly.GetTypes()
            .Where(T => T.IsClass &&
            !T.IsAbstract &&
            T.IsSubclassOf(targetType)))
        {

          object[] nodeProperties = type.GetCustomAttributes(typeof(BonsaiNodeAttribute), false);

          // The attribute is to simply get custom data about the node.
          // Like menu path and texture.
          BonsaiNodeAttribute attrib = null;
          if (nodeProperties.Length > 0)
          {
            attrib = nodeProperties[0] as BonsaiNodeAttribute;
          }

          string menuPath = "Uncategorized/";

          // Service base class is abstract. For Service types, default to Service texture.
          string texName = typeof(Service).IsAssignableFrom(type) ? "Service" : "Play";

          if (attrib != null)
          {
            if (!string.IsNullOrEmpty(attrib.menuPath))
            {
              menuPath = attrib.menuPath;
            }

            // Texxture names are optional. Use only if specified.
            if (!string.IsNullOrEmpty(attrib.texturePath))
            {
              texName = attrib.texturePath;
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