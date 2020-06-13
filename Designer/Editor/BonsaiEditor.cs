
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
  internal class BonsaiEditor
  {
    private readonly BonsaiWindow window;
    public BonsaiCanvas Canvas { get; private set; }
    public Coord Coordinates { get; private set; }

    private static Dictionary<Type, NodeTypeProperties> _behaviourNodes;

    private BonsaiNode _nodeToPositionUnderMouse = null;

    // Remembers which nodes are currently being referenced by another node.
    private HashSet<BehaviourNode> _referencedNodes = new HashSet<BehaviourNode>();

    // The types of node which can store refs to other nodes.
    internal HashSet<Type> referenceContainerTypes = new HashSet<Type>();

    /// <summary>
    /// The unit length of the grid in pixels.
    /// Note: Grid Texture has 12.8 as length, fix texture to be even.
    /// </summary>
    public const float kGridSize = 12f;

    /// <summary>
    /// The multiple that grid snapping rounds to.
    /// It should be a multiple of the grid size.
    /// </summary>
    public static float snapStep = kGridSize;

    public BonsaiEditor(BonsaiWindow window)
    {
      this.window = window;
      referenceContainerTypes.Add(typeof(Interruptor));
      referenceContainerTypes.Add(typeof(Guard));
    }

    public void SetBehaviourTree(BehaviourTree tree)
    {
      Canvas = new BonsaiCanvas(tree);
      Coordinates = new Coord(Canvas, window);
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

    #region Node/Canvas Editing and Modification

    /// <summary>
    /// Translates the canvas.
    /// </summary>
    /// <param name="delta">The amount to translate the canvas.</param>
    public void Pan(Vector2 delta)
    {
      Canvas.panOffset += delta * Canvas.ZoomScale * BonsaiCanvas.panSpeed;

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
      float scale = (zoomDirection < 0f) ? (1f - BonsaiCanvas.zoomDelta) : (1f + BonsaiCanvas.zoomDelta);
      Canvas.zoom *= scale;

      float cap = Mathf.Clamp(Canvas.zoom.x, BonsaiCanvas.minZoom, BonsaiCanvas.maxZoom);
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

        float nodeTop = root.Input.bodyRect.yMin;
        float parentBottom = root.Input.outputConnection.bodyRect.yMax;

        // The root cannot be above its parent.
        if (nodeTop < parentBottom)
        {
          min = parentBottom;
        }
      }

      // Record the old position so we can know by how much the root moved
      // so all children can be shifted by the pan delta.
      Vector2 oldPos = root.bodyRect.center;

      // Clamp the position so it does not go above the parent.
      Vector2 diff = pos - offset;
      diff.y = Mathf.Clamp(diff.y, min, float.MaxValue);

      Vector2 rounded = Coord.SnapPosition(diff, snapStep);
      root.bodyRect.center = rounded;

      // Calculate the change of position of the root.
      Vector2 pan = root.bodyRect.center - oldPos;

      // Move the entire subtree of the root.
      Action<BonsaiNode> subtreeDrag = (node) =>
      {
        // For all children, pan by the same amount that the parent changed by.
        if (node != root)
          node.bodyRect.center += Coord.SnapPosition(pan, snapStep);
      };

      TreeIterator<BonsaiNode>.Traverse(root, subtreeDrag);
    }

    internal void UpdateOrderIndices()
    {
      window.tree.CalculateTreeOrders();
    }

    internal void SetReferencedNodes(IEnumerable<BehaviourNode> nodes)
    {
      if (nodes == null)
      {
        return;
      }

      _referencedNodes.Clear();
      foreach (BehaviourNode node in nodes)
      {
        _referencedNodes.Add(node);
      }
    }

    internal void ClearReferencedNodes()
    {
      _referencedNodes.Clear();
    }

    #endregion

    #region Aggregate Drawing Methods (Draw all nodes, all ports, ...etc)

    public void DrawStaticGrid()
    {
      Drawer.DrawStaticGrid(window.CanvasRect, BonsaiPreferences.Instance.gridTexture);
    }

    private void DrawGrid()
    {
      Drawer.DrawGrid(window.CanvasRect, BonsaiPreferences.Instance.gridTexture, Canvas.ZoomScale, Canvas.panOffset);
    }

    private void DrawCanvasContents()
    {
      ScaleUtility.BeginScale(window.CanvasRect, Canvas.ZoomScale, window.toolbarHeight);

      DrawConnectionPreview();
      DrawPortConnections();
      DrawNodes();

      ScaleUtility.EndScale(window.CanvasRect, Canvas.ZoomScale, window.toolbarHeight);

      // Selection overlays and independent of zoom.
      DrawAreaSelection();
    }

    private void DrawNodes()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        DrawNode(node);
        DrawPorts(node);
      }
    }

    private void DrawPortConnections()
    {
      foreach (var node in Canvas.NodesInDrawOrder)
      {
        if (node.Output != null)
        {
          DrawDefaultPortConnections(node);
        }
      }
    }

    private void DrawDefaultPortConnections(BonsaiNode node)
    {
      Color connectionColor = BonsaiPreferences.Instance.defaultConnectionColor;
      float connectionWidth = BonsaiPreferences.Instance.defaultConnectionWidth;

      if (node.Behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running)
      {
        connectionColor = BonsaiPreferences.Instance.runningStatusColor;
        connectionWidth = BonsaiPreferences.Instance.runningConnectionWidth;
      }

      // Start the Y anchor coord at the tip of the Output port.
      float yoffset = node.bodyRect.yMax;

      // Calculate the anchor position.
      float anchorX = node.bodyRect.center.x;
      float anchorY = (yoffset + node.Output.GetNearestInputY()) / 2f;

      // Anchor line, between the first and last child.

      // Find the min and max X coords between the children and the parent.
      node.Output.GetBoundsX(out float anchorLineStartX, out float anchorLineEndX);

      // Get start and end positions of the anchor line (The common line where the parent and children connect).
      var anchorLineStart = new Vector2(anchorLineStartX, anchorY);
      var anchorLineEnd = new Vector2(anchorLineEndX, anchorY);

      // The tip where the parent starts its line to connect to the anchor line.
      var parentAnchorTip = new Vector2(anchorX, yoffset);

      // The point where the parent connects to the anchor line.
      var parentAnchorLineConnection = new Vector2(anchorX, anchorY);

      // Draw the lines from the calculated positions.
      DrawLineCanvasSpace(parentAnchorTip, parentAnchorLineConnection, connectionColor, connectionWidth);
      DrawLineCanvasSpace(anchorLineStart, anchorLineEnd, BonsaiPreferences.Instance.defaultConnectionColor, BonsaiPreferences.Instance.defaultConnectionWidth);

      foreach (var input in node.Output.InputConnections)
      {
        // Get the positions to draw a line between the node and the anchor line.
        Vector2 center = input.bodyRect.center;
        var anchorLineConnection = new Vector2(center.x, anchorY);

        // The node is running, hightlight the connection.
        if (input.parentNode.Behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running)
        {
          DrawLineCanvasSpace(center, anchorLineConnection, BonsaiPreferences.Instance.runningStatusColor, BonsaiPreferences.Instance.runningConnectionWidth);

          // Hightlight the portion of the anchorline between the running child and parent node.
          DrawLineCanvasSpace(anchorLineConnection, parentAnchorLineConnection, BonsaiPreferences.Instance.runningStatusColor, BonsaiPreferences.Instance.runningConnectionWidth);
        }
        else
        {
          // The node is not running, draw a default connection.
          DrawLineCanvasSpace(center, anchorLineConnection, BonsaiPreferences.Instance.defaultConnectionColor, BonsaiPreferences.Instance.defaultConnectionWidth);
        }
      }
    }

    #endregion

    #region Canvas Unit Drawing Methods (Node, Ports, ... etc)

    private void DrawNode(BonsaiNode node)
    {
      // Convert the node rect from canvas to screen space.
      Rect screenRect = node.bodyRect;
      screenRect.position = Coordinates.CanvasToScreenSpace(screenRect.position);

      // Remember the original color that way it is reset when the function exits.
      Color originalColor = GUI.color;

      DrawNodeBackground(node, screenRect);

      // The node contents are grouped together within the node body.
      GUI.BeginGroup(screenRect);

      // Make the body of node local to the group coordinate space.
      Rect localRect = node.bodyRect;
      localRect.position = Vector2.zero;

      // Draw the status the node exited with if applicable.
      DrawExitStatus(localRect, node);

      // Draw the contents inside the node body, automatically laid out.
      GUILayout.BeginArea(localRect, GUIStyle.none);

      DrawNodeTypeBackground(node);
      DrawNodeContent(node);

      GUILayout.EndArea();

      GUI.EndGroup();
      GUI.color = originalColor;
    }

    private void DrawNodeBackground(BonsaiNode node, Rect screenRect)
    {
      GUI.DrawTexture(
        screenRect, BonsaiPreferences.Instance.nodeBackgroundTexture,
        ScaleMode.StretchToFill,
        true,
        0,
        NodeStatusColor(node),
        0,
        5f);
    }

    // Render the background color scheme for the node type.
    private void DrawNodeTypeBackground(BonsaiNode node)
    {
      GUI.DrawTexture(
        node.ContentRect,
        BonsaiPreferences.Instance.nodeGradient,
        ScaleMode.StretchToFill,
        true,
        0f,
        NodeTypeColor(node),
        0f,
        4f);
    }

    // Render the node body contents.
    private void DrawNodeContent(BonsaiNode node)
    {
      // Spacing for input.
      if (node.Input != null)
      {
        GUILayout.Space(node.Input.bodyRect.height);
      }

      GUILayout.Box(node.HeaderContent, node.HeaderStyle);
      GUILayout.Box(node.BodyContent, node.BodyStyle);
    }

    private void DrawExitStatus(Rect localRect, BonsaiNode node)
    {
      var status = node.Behaviour.GetStatusEditor();

      if (status == BehaviourNode.StatusEditor.Success)
      {
        DrawTexture(localRect, BonsaiPreferences.Instance.successSymbol, BonsaiPreferences.Instance.successColor);
      }

      else if (status == BehaviourNode.StatusEditor.Failure)
      {
        DrawTexture(localRect, BonsaiPreferences.Instance.failureSymbol, BonsaiPreferences.Instance.failureColor);
      }

      else if (status == BehaviourNode.StatusEditor.Aborted)
      {
        DrawTexture(localRect, BonsaiPreferences.Instance.failureSymbol, BonsaiPreferences.Instance.abortedColor);
      }

      else if (status == BehaviourNode.StatusEditor.Interruption)
      {
        DrawTexture(localRect, BonsaiPreferences.Instance.failureSymbol, BonsaiPreferences.Instance.interruptedColor);
      }
    }

    private Color NodeTypeColor(BonsaiNode node)
    {
      if (node.Behaviour is Task)
      {
        return BonsaiPreferences.Instance.taskColor;
      }

      else if (node.Behaviour is Service)
      {
        return BonsaiPreferences.Instance.serviceColor;
      }

      else if (node.Behaviour is ConditionalAbort)
      {
        return BonsaiPreferences.Instance.conditionalColor;
      }

      else if (node.Behaviour is Decorator)
      {
        return BonsaiPreferences.Instance.decoratorColor;
      }

      return BonsaiPreferences.Instance.compositeColor;
    }

    private Color NodeStatusColor(BonsaiNode node)
    {

      if (IsNodeEvaluating(node))
      {
        return BonsaiPreferences.Instance.evaluateColor;
      }
      else if (IsNodeRunning(node))
      {
        return BonsaiPreferences.Instance.runningColor;
      }
      else if (IsNodeSelected(node))
      {
        return BonsaiPreferences.Instance.selectedColor;
      }

      else if (IsNodeReferenced(node))
      {
        return BonsaiPreferences.Instance.referenceColor;
      }
      else if (IsNodeAbortable(node))
      {
        return BonsaiPreferences.Instance.abortColor;
      }

      else if (window.tree.Root == node.Behaviour)
      {
        return BonsaiPreferences.Instance.rootSymbolColor;
      }

      return BonsaiPreferences.Instance.defaultNodeBackgroundColor;
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
      if (!window.tree.Root)
      {
        return false;
      }

      BonsaiNode selected = window.inputHandler.SelectedNode;

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
      return _referencedNodes.Contains(node.Behaviour);
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
          BehaviourNode currentNode = window.tree.GetNode(index);

          // The current running node can be aborted by it.
          return aborter && ConditionalAbort.IsAbortable(aborter, currentNode);
        }
      }

      return false;
    }

    // Helper method to draw textures with color tint.
    private static void DrawTexture(Rect r, Texture2D tex, Color c)
    {
      var originalColor = GUI.color;

      GUI.color = c;
      GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);

      GUI.color = originalColor;
    }

    private void DrawPorts(BonsaiNode node)
    {
      Rect nodeRect = node.bodyRect;
      BonsaiOutputPort output = node.Output;
      BonsaiInputPort input = node.Input;

      if (input != null)
      {
        input.bodyRect.width = nodeRect.width - BonsaiNode.kContentOffset.x * 2f;

        // Place the port above the node
        float x = nodeRect.x + (nodeRect.width - input.bodyRect.width) / 2f;
        float y = nodeRect.yMin;
        input.bodyRect.position = new Vector2(x, y);

        DrawPort(input.bodyRect);
      }

      if (output != null)
      {
        output.bodyRect.width = nodeRect.width - BonsaiNode.kContentOffset.x * 2f;

        // Place the port below the node.
        float x = nodeRect.x + (nodeRect.width - output.bodyRect.width) / 2f;
        float y = nodeRect.yMax - output.bodyRect.height;
        output.bodyRect.position = new Vector2(x, y);

        DrawPort(output.bodyRect);
      }
    }

    private void DrawPort(Rect portRect)
    {
      // Convert the body rect from canvas to screen space.
      portRect.position = Coordinates.CanvasToScreenSpace(portRect.position);
      GUI.DrawTexture(portRect, BonsaiPreferences.Instance.portTexture, ScaleMode.StretchToFill);
    }

    /// <summary>
    /// Draws the preview connection from the selected output port and the mouse.
    /// </summary>
    private void DrawConnectionPreview()
    {
      // Draw connection between mouse and the port.
      if (window.inputHandler.IsMakingConnection)
      {
        var start = Coordinates.CanvasToScreenSpace(window.inputHandler.OutputToConnect.bodyRect.center);
        var end = Event.current.mousePosition;
        DrawRectConnectionScreenSpace(start, end, Color.white);
        window.Repaint();
      }
    }

    /// <summary>
    /// Handles drawing a rect line between two points in canvas space.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public void DrawRectConnectionCanvasSpace(Vector2 start, Vector2 end, Color color)
    {
      start = Coordinates.CanvasToScreenSpace(start);
      end = Coordinates.CanvasToScreenSpace(end);

      DrawRectConnectionScreenSpace(start, end, color);
    }

    /// <summary>
    /// Handles drawing a rect line between two points in screen space.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public void DrawRectConnectionScreenSpace(Vector2 start, Vector2 end, Color color)
    {
      var originalColor = Handles.color;
      Handles.color = color;

      // The distance between start and end halved.
      float halfDist = (start - end).magnitude / 2f;

      Vector2 directionToEnd = (end - start).normalized;
      Vector2 directionToStart = (start - end).normalized;

      // This dictates in what direction are the tips aligned.
      // If Vector.up, then the tips are aligned on the y axis and the middle line
      // is aligned on the x-axis(right).
      //
      //If Vector.right, then the tips are aligned on the x axis and the middle line
      // is aligned on the y-axis(up).
      Vector2 axisForTipAlignment = Vector3.up;

      // Project the directions to the towards the end/start positions along the
      // axis of alignment.
      Vector2 startTip = Vector3.Project(directionToEnd, axisForTipAlignment) * halfDist + (Vector3)start;
      Vector2 endTip = Vector3.Project(directionToStart, axisForTipAlignment) * halfDist + (Vector3)end;

      if (startTip == endTip)
      {
        Handles.DrawLine(start, end);
      }

      else
      {
        Handles.DrawLine(start, startTip);
        Handles.DrawLine(end, endTip);
        Handles.DrawLine(startTip, endTip);
      }

      Handles.color = originalColor;
    }

    public void DrawLineCanvasSpace(Vector2 start, Vector2 end, Color color)
    {
      start = Coordinates.CanvasToScreenSpace(start);
      end = Coordinates.CanvasToScreenSpace(end);

      DrawLineScreenSpace(start, end, color);
    }

    public void DrawLineScreenSpace(Vector2 start, Vector2 end, Color color)
    {
      var originalColor = Handles.color;
      Handles.color = color;

      Handles.DrawLine(start, end);

      Handles.color = originalColor;
    }

    public void DrawLineCanvasSpace(Vector2 start, Vector2 end, Color color, float width)
    {
      start = Coordinates.CanvasToScreenSpace(start);
      end = Coordinates.CanvasToScreenSpace(end);

      DrawLineScreenSpace(start, end, color, width);
    }

    public void DrawLineScreenSpace(Vector2 start, Vector2 end, Color color, float width)
    {
      var originalColor = Handles.color;
      Handles.color = color;

      Handles.DrawAAPolyLine(width, start, end);

      Handles.color = originalColor;
    }

    private void DrawAreaSelection()
    {
      if (window.inputHandler.IsAreaSelecting)
      {
        // Construct and display the rect.
        Rect selectionRect = window.inputHandler.SelectionScreenSpace();
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
      if (!window.tree)
      {
        GUI.Label(_modeStatusRect, new GUIContent("No Tree Set"), ModeStatusStyle);
      }

      else if (window.inputHandler.IsRefLinking)
      {
        GUI.Label(_modeStatusRect, new GUIContent("Link References"), ModeStatusStyle);
      }

      else if (window.GetMode() == BonsaiWindow.Mode.Edit)
      {
        GUI.Label(_modeStatusRect, new GUIContent("Edit"), ModeStatusStyle);
      }

      else
      {
        GUI.Label(_modeStatusRect, new GUIContent("View"), ModeStatusStyle);
      }
    }

    #endregion

    private void HandleNewNodeToPositionUnderMouse()
    {
      if (_nodeToPositionUnderMouse != null)
      {
        _nodeToPositionUnderMouse.bodyRect.position = Coordinates.MousePosition();
        _nodeToPositionUnderMouse = null;
      }
    }

    internal void SetNewNodeToPositionUnderMouse(BonsaiNode node)
    {
      _nodeToPositionUnderMouse = node;
    }

    private GUIStyle _modeStatusStyle;

    private Rect _modeStatusRect = new Rect(20f, 20f, 250f, 150f);

    private GUIStyle ModeStatusStyle
    {
      get
      {
        if (_modeStatusStyle == null)
        {
          _modeStatusStyle = new GUIStyle
          {
            fontSize = 36,
            fontStyle = FontStyle.Bold
          };
          _modeStatusStyle.normal.textColor = new Color(1f, 1f, 1f, 0.2f);
        }

        return _modeStatusStyle;
      }
    }

    #region Node Type Properties

    public static void FetchBehaviourNodes()
    {
      _behaviourNodes = new Dictionary<Type, NodeTypeProperties>();

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
          string texName = "Play";

          if (attrib != null)
          {
            menuPath = attrib.menuPath;
            texName = attrib.texturePath;
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

          _behaviourNodes.Add(type, prop);
        }
      }
    }

    public static IEnumerable<KeyValuePair<Type, NodeTypeProperties>> Behaviours
    {
      get { return _behaviourNodes; }
    }

    public static NodeTypeProperties GetNodeTypeProperties(Type t)
    {
      if (_behaviourNodes.ContainsKey(t))
      {
        return _behaviourNodes[t];
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