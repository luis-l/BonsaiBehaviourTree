
using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Bonsai.Core;
using Bonsai.Standard;

namespace Bonsai.Designer
{
  /// <summary>
  /// The editor handles rendering, manipulation and viewing of the canvas.
  /// </summary>
  internal class BonsaiEditor
  {
    public BonsaiWindow window;
    internal BonsaiCanvas canvas;

    private static Dictionary<Type, NodeTypeProperties> _behaviourNodes;

    private BonsaiNode _nodeToPositionUnderMouse = null;

    private Texture2D _backgroundTex;
    private Texture2D _rootSymbol;
    private Texture2D _failureSymbol;
    private Texture2D _successSymbol;

    private Texture2D _lowerPrioritySymbol;
    private Texture2D _selfPrioritySymbol;
    private Texture2D _bothPrioritySymbol;

    private Texture2D _defaultBackgroundTex;
    private Texture2D _selectedHighlightTex;
    private Texture2D _runningBackgroundTex;
    private Texture2D _abortHighlightTex;
    private Texture2D _referenceHighlightTex;
    private Texture2D _reevaluateHighlightTex;

    private Color _rootSymbolColor;
    private Color _runningStatusColor;
    private Color _successColor;
    private Color _failureColor;
    private Color _abortedColor;
    private Color _interruptedColor;

    private Color _defaultConnectionColor = Color.white;
    private float _defaultConnectionWidth = 3f;
    private float _runningConnectionWidth = 5f;

    private Vector2 _abortIconOffset = new Vector2(2f, 4f);
    private Vector2 _abortIconSize = Vector2.one * 20f;

    private GUIStyle _backgroundStyle;
    private GUIStyle backgroundStyle
    {
      get
      {
        if (_backgroundStyle == null)
        {
          _backgroundStyle = new GUIStyle(GUI.skin.box);
          _backgroundStyle.normal.background = BonsaiResources.GetTexture("GrayGradient");
        }

        return _backgroundStyle;
      }
    }

    // Remembers which nodes are currently being referenced by another node.
    private HashSet<BehaviourNode> _referencedNodes = new HashSet<BehaviourNode>();

    // The types of node which can store refs to other nodes.
    internal HashSet<Type> referenceContainerTypes = new HashSet<Type>();

    /// <summary>
    /// The unit length of the grid in pixels.
    /// </summary>
    public const float kGridSize = 12.8f;

    /// <summary>
    /// The multiple that grid snapping rounds to.
    /// It should be a multiple of the grid size.
    /// </summary>
    public static float snapStep = kGridSize;

    public BonsaiEditor()
    {
      CacheTextures();

      _rootSymbolColor = new Color(0.3f, 0.3f, 0.3f, 1f);
      _runningStatusColor = new Color(0.1f, 1f, 0.54f, 1f);
      _successColor = new Color(0.1f, 1f, 0.54f, 0.25f);
      _failureColor = new Color(1f, 0.1f, 0.1f, 0.25f);
      _abortedColor = new Color(0.1f, 0.1f, 1f, 0.25f);
      _interruptedColor = new Color(0.7f, 0.5f, 0.3f, 0.4f);

      referenceContainerTypes.Add(typeof(Interruptor));
      referenceContainerTypes.Add(typeof(Guard));
    }

    public void Draw()
    {
      // We need to wait for the generic menu to finish
      // in order to get a valid mouse position.
      handleNewNodeToPositionUnderMouse();

      if (Event.current.type == EventType.Repaint)
      {
        drawGrid();
        DrawMode();
      }

      drawCanvasContents();
    }

    public void CacheTextures()
    {
      _backgroundTex = BonsaiResources.GetTexture("Grid");
      _rootSymbol = BonsaiResources.GetTexture("RootSymbol");
      _successSymbol = BonsaiResources.GetTexture("Checkmark");
      _failureSymbol = BonsaiResources.GetTexture("Cross");

      _lowerPrioritySymbol = BonsaiResources.GetTexture("RightChevron");
      _selfPrioritySymbol = BonsaiResources.GetTexture("BottomChevron");
      _bothPrioritySymbol = BonsaiResources.GetTexture("DoubleChevron");

      _defaultBackgroundTex = BonsaiResources.GetTexture("GrayGradient");
      _selectedHighlightTex = BonsaiResources.GetTexture("GrayGradientFresnel");
      _runningBackgroundTex = BonsaiResources.GetTexture("GreenGradient");

      _abortHighlightTex = BonsaiResources.GetTexture("AbortHighlightGradient");
      _referenceHighlightTex = BonsaiResources.GetTexture("ReferenceHighlightGradient");
      _reevaluateHighlightTex = BonsaiResources.GetTexture("ReevaluateHighlightGradient");
    }

    #region Node/Canvas Editing and Modification

    /// <summary>
    /// Translates the canvas.
    /// </summary>
    /// <param name="delta">The amount to translate the canvas.</param>
    public void Pan(Vector2 delta)
    {
      canvas.panOffset += delta * canvas.ZoomScale * BonsaiCanvas.panSpeed;
    }

    /// <summary>
    /// Scales the canvas.
    /// </summary>
    /// <param name="zoomDirection">+1 if you want to zoom in and -1 if you want to zoom out.</param>
    public void Zoom(float zoomDirection)
    {
      float scale = (zoomDirection < 0f) ? (1f - BonsaiCanvas.zoomDelta) : (1f + BonsaiCanvas.zoomDelta);
      canvas.zoom *= scale;

      float cap = Mathf.Clamp(canvas.zoom.x, BonsaiCanvas.minZoom, BonsaiCanvas.maxZoom);
      canvas.zoom.Set(cap, cap);
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
      Vector2 oldPos = root.bodyRect.position;

      // Clamp the position so it does not go above the parent.
      Vector2 diff = pos - offset;
      diff.y = Mathf.Clamp(diff.y, min, float.MaxValue);

      Vector2 rounded = SnapPosition(diff);
      root.bodyRect.position = rounded;

      // Calculate the change of position of the root.
      Vector2 pan = root.bodyRect.position - oldPos;

      // Move the entire subtree of the root.
      Action<BonsaiNode> subtreeDrag = (node) =>
      {
        // For all children, pan by the same amount that the parent changed by.
        if (node != root)
          node.bodyRect.position += SnapPosition(pan);
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

    #region Aggregate Drawing Methods (Draw all nodes, all knobs, ...etc)

    /// <summary>
    /// Draws a static grid that is unaffected by zoom and pan.
    /// </summary>
    public void DrawStaticGrid()
    {
      var size = window.CanvasRect.size;
      var center = size / 2f;

      float xOffset = -center.x / _backgroundTex.width;
      float yOffset = (center.y - size.y) / _backgroundTex.height;

      // Offset from origin in tile units
      Vector2 tileOffset = new Vector2(xOffset, yOffset);

      float tileAmountX = Mathf.Round(size.x) / _backgroundTex.width;
      float tileAmountY = Mathf.Round(size.y) / _backgroundTex.height;

      // Amount of tiles
      Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

      // Draw tiled background
      var backRect = window.CanvasRect;
      GUI.DrawTextureWithTexCoords(backRect, _backgroundTex, new Rect(tileOffset, tileAmount));
    }

    private void drawGrid()
    {
      var size = window.CanvasRect.size;
      var center = size / 2f;

      float zoom = canvas.ZoomScale;

      // Offset from origin in tile units
      float xOffset = -(center.x * zoom + canvas.panOffset.x) / _backgroundTex.width;
      float yOffset = ((center.y - size.y) * zoom + canvas.panOffset.y) / _backgroundTex.height;

      Vector2 tileOffset = new Vector2(xOffset, yOffset);

      // Amount of tiles
      float tileAmountX = Mathf.Round(size.x * zoom) / _backgroundTex.width;
      float tileAmountY = Mathf.Round(size.y * zoom) / _backgroundTex.height;

      Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

      // Draw tiled background
      GUI.DrawTextureWithTexCoords(window.CanvasRect, _backgroundTex, new Rect(tileOffset, tileAmount));
    }

    private void drawCanvasContents()
    {
      ScaleUtility.BeginScale(window.CanvasRect, canvas.ZoomScale, window.toolbarHeight);

      drawConnectionPreview();
      drawKnobConnections();
      drawNodes();

      ScaleUtility.EndScale(window.CanvasRect, canvas.ZoomScale, window.toolbarHeight);

      // Selection overlays and independent of zoom.
      drawAreaSelection();
    }

    private void drawNodes()
    {
      foreach (var node in canvas.NodesInDrawOrder)
      {
        drawKnobs(node);
        drawNode(node);
      }
    }

    private void drawKnobConnections()
    {
      foreach (var node in canvas.NodesInDrawOrder)
      {

        if (node.Output != null)
        {
          drawDefaultKnobConnections(node);
        }
      }
    }

    private void drawDefaultKnobConnections(BonsaiNode node)
    {
      Color connectionColor = _defaultConnectionColor;
      float connectionWidth = _defaultConnectionWidth;

      if (node.behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running)
      {
        connectionColor = _runningStatusColor;
        connectionWidth = _runningConnectionWidth;
      }

      // Start the Y anchor coord at the tip of the Output knob.
      float yoffset = node.bodyRect.yMax + BonsaiKnob.kMinSize.y;

      // Calculate the anchor position.
      float anchorX = node.bodyRect.center.x;
      float anchorY = (yoffset + node.Output.GetNearestInputY()) / 2f;

      // Anchor line, between the first and last child.
      float anchorLineStartX;
      float anchorLineEndX;

      // Find the min and max X coords between the children and the parent.
      node.Output.GetBoundsX(out anchorLineStartX, out anchorLineEndX);

      // Get start and end positions of the anchor line (The common line where the parent and children connect).
      var anchorLineStart = new Vector2(anchorLineStartX, anchorY);
      var anchorLineEnd = new Vector2(anchorLineEndX, anchorY);

      // The tip where the parent starts its line to connect to the anchor line.
      var parentAnchorTip = new Vector2(anchorX, yoffset);

      // The point where the parent connects to the anchor line.
      var parentAnchorLineConnection = new Vector2(anchorX, anchorY);

      // Draw the lines from the calculated positions.
      DrawLineCanvasSpace(parentAnchorTip, parentAnchorLineConnection, connectionColor, connectionWidth);
      DrawLineCanvasSpace(anchorLineStart, anchorLineEnd, _defaultConnectionColor, _defaultConnectionWidth);

      foreach (var input in node.Output.InputConnections)
      {

        // Get the positions to draw a line between the node and the anchor line.
        Vector2 center = input.bodyRect.center;
        var anchorLineConnection = new Vector2(center.x, anchorY);

        // The node is running, hightlight the connection.
        if (input.parentNode.behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running)
        {

          DrawLineCanvasSpace(center, anchorLineConnection, _runningStatusColor, _runningConnectionWidth);

          // Hightlight the portion of the anchorline between the running child and parent node.
          DrawLineCanvasSpace(anchorLineConnection, parentAnchorLineConnection, _runningStatusColor, _runningConnectionWidth);
        }

        // The node is not running, draw a default connection.
        else
        {
          DrawLineCanvasSpace(center, anchorLineConnection, _defaultConnectionColor, _defaultConnectionWidth);
        }
      }
    }

    #endregion

    #region Canvas Unit Drawing Methods (Node, Knobs, ... etc)

    private void drawNode(BonsaiNode node)
    {
      // Convert the node rect from canvas to screen space.
      Rect screenRect = node.bodyRect;
      screenRect.position = CanvasToScreenSpace(screenRect.position);

      // Remember the original color that way it is reset when the function exits.
      Color originalColor = GUI.color;

      // Set the default background.
      backgroundStyle.normal.background = _defaultBackgroundTex;

      highlightAsAbortable(node);
      highlightAsReferenced(node);
      highlightAsSelected(node);
      highlightAsRunning(node);
      highlightAsReevaluated(node);

      // The node contents are grouped together within the node body.
      GUI.BeginGroup(screenRect, backgroundStyle);

      // Make the body of node local to the group coordinate space.
      Rect localRect = node.bodyRect;
      localRect.position = Vector2.zero;

      // Add root symbol if applicable
      drawRootSymbol(localRect, node);

      // Draw the status the node exited with if applicable.
      drawExitStatus(localRect, node);

      // Draw the conditional abort of the node if applicable.
      drawAbortPriorityIcon(localRect, node);

      // Draw the contents inside the node body, automatically laidout.
      GUILayout.BeginArea(localRect, GUIStyle.none);

      GUILayout.Box(node.IconNameContent, node.IconNameStyle);

      GUILayout.EndArea();
      GUI.EndGroup();
      GUI.color = originalColor;
    }

    private void drawRootSymbol(Rect localRect, BonsaiNode node)
    {
      if (window.tree.Root == node.behaviour)
      {

        // Shift the symbol so it is not in the center.
        localRect.x -= localRect.width / 2f - 20f;

        drawTexture(localRect, _rootSymbol, _rootSymbolColor);
      }
    }

    private void highlightAsRunning(BonsaiNode node)
    {
      // Set the color to show that the node is running.
      if (node.behaviour.GetStatusEditor() == BehaviourNode.StatusEditor.Running)
      {
        backgroundStyle.normal.background = _runningBackgroundTex;
      }
    }

    private void drawExitStatus(Rect localRect, BonsaiNode node)
    {
      var status = node.behaviour.GetStatusEditor();

      if (status == BehaviourNode.StatusEditor.Success)
      {
        drawTexture(localRect, _successSymbol, _successColor);
      }

      else if (status == BehaviourNode.StatusEditor.Failure)
      {
        drawTexture(localRect, _failureSymbol, _failureColor);
      }

      else if (status == BehaviourNode.StatusEditor.Aborted)
      {
        drawTexture(localRect, _failureSymbol, _abortedColor);
      }

      else if (status == BehaviourNode.StatusEditor.Interruption)
      {
        drawTexture(localRect, _failureSymbol, _interruptedColor);
      }
    }

    private void drawAbortPriorityIcon(Rect localRect, BonsaiNode node)
    {
      var abortNode = node.behaviour as ConditionalAbort;

      // Can only show icon for abort nodes.
      if (abortNode && abortNode.abortType != AbortType.None)
      {

        localRect.position = _abortIconOffset;
        localRect.size = _abortIconSize;

        Texture2D tex = null;

        switch (abortNode.abortType)
        {

          case AbortType.LowerPriority:
            tex = _lowerPrioritySymbol;
            break;

          case AbortType.Self:

            // Offset a bit so it is not too close to left edge.
            localRect.x += 2f;
            tex = _selfPrioritySymbol;
            break;

          case AbortType.Both:
            tex = _bothPrioritySymbol;
            break;
        }

        GUILayout.BeginArea(localRect, tex);
        GUILayout.EndArea();
      }
    }

    // Highlights nodes that can be aborted by the currently selected node.
    private void highlightAsAbortable(BonsaiNode node)
    {
      // Root must exist.
      if (!window.tree.Root) return;

      BonsaiNode selected = window.inputHandler.SelectedNode;

      // A node must be selected.
      if (selected == null) return;

      var aborter = selected.behaviour as ConditionalAbort;

      // The selected node must be a conditional abort.
      if (aborter)
      {

        // Highlight this node if it can be aborted by the selected aborter.
        if (ConditionalAbort.IsAbortable(aborter, node.behaviour))
        {
          backgroundStyle.normal.background = _abortHighlightTex;
        }
      }
    }

    // Nodes that are being referenced are highlighted.
    private void highlightAsReferenced(BonsaiNode node)
    {
      if (_referencedNodes.Contains(node.behaviour))
      {
        backgroundStyle.normal.background = _referenceHighlightTex;
      }
    }

    private void highlightAsSelected(BonsaiNode node)
    {
      // Highlight if node is selected.
      if (node.behaviour == Selection.activeObject || node.bAreaSelectionFlag)
      {
        backgroundStyle.normal.background = _selectedHighlightTex;
      }
    }

    /// <summary>
    /// Highlights nodes that are being re-evaluated, like abort nodes or
    /// children under reactive parents.
    /// </summary>
    /// <param name="node"></param>
    private void highlightAsReevaluated(BonsaiNode node)
    {
      if (!EditorApplication.isPlaying)
      {
        return;
      }

      BehaviourIterator itr = node.behaviour.Iterator;

      if (itr != null && itr.IsRunning)
      {

        var aborter = node.behaviour as ConditionalAbort;
        int index = itr.CurrentIndex;
        if (index != -1)
        {
          BehaviourNode currentNode = window.tree.GetNode(index);

          // Only highlight the abort if the current running node can be aborted by it.
          if (aborter && ConditionalAbort.IsAbortable(aborter, currentNode))
          {
            backgroundStyle.normal.background = _reevaluateHighlightTex;
          }
        }

      }
    }

    // Helper method to draw textures with color tint.
    private static void drawTexture(Rect r, Texture2D tex, Color c)
    {
      var originalColor = GUI.color;

      GUI.color = c;
      GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);

      GUI.color = originalColor;
    }

    private void drawKnobs(BonsaiNode node)
    {
      var bodyRect = node.bodyRect;
      var output = node.Output;
      var input = node.Input;

      if (input != null)
      {

        float x = bodyRect.x + (bodyRect.width - input.bodyRect.width) / 2f;
        float y = bodyRect.y - input.bodyRect.height;

        input.bodyRect.position = new Vector2(x, y);
        drawKnob(input);
      }

      if (output != null)
      {

        float x = bodyRect.x + (bodyRect.width - output.bodyRect.width) / 2f;
        float y = bodyRect.y + bodyRect.height;

        output.bodyRect.position = new Vector2(x, y);
        drawKnob(output);
      }
    }

    private void drawKnob(BonsaiKnob knob)
    {
      // Convert the body rect from canvas to screen space.
      var screenRect = knob.bodyRect;
      screenRect.position = CanvasToScreenSpace(screenRect.position);

      if (knob.background)
      {
        GUI.DrawTexture(screenRect, knob.background);
      }
    }

    /// <summary>
    /// Draws the preview connection from the selected output knob and the mouse.
    /// </summary>
    private void drawConnectionPreview()
    {
      // Draw connection between mouse and the knob.
      if (window.inputHandler.IsMakingConnection)
      {
        var start = CanvasToScreenSpace(window.inputHandler.OutputToConnect.bodyRect.center);
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
      start = CanvasToScreenSpace(start);
      end = CanvasToScreenSpace(end);

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
      start = CanvasToScreenSpace(start);
      end = CanvasToScreenSpace(end);

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
      start = CanvasToScreenSpace(start);
      end = CanvasToScreenSpace(end);

      DrawLineScreenSpace(start, end, color, width);
    }

    public void DrawLineScreenSpace(Vector2 start, Vector2 end, Color color, float width)
    {
      var originalColor = Handles.color;
      Handles.color = color;

      Handles.DrawAAPolyLine(width, start, end);

      Handles.color = originalColor;
    }

    private void drawAreaSelection()
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

    #region Space Transformations and Mouse Utilities

    /// <summary>
    /// Rounds the position to the nearest grid coordinate.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Vector2 SnapPosition(Vector2 p)
    {
      return SnapPosition(p.x, p.y);
    }

    /// <summary>
    /// Rounds the position to the nearest grid coordinate.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public Vector2 SnapPosition(float x, float y)
    {
      x = Mathf.Round(x / snapStep) * snapStep;
      y = Mathf.Round(y / snapStep) * snapStep;

      return new Vector2(x, y);
    }

    /// <summary>
    /// Returns the mouse position in canvas space.
    /// </summary>
    /// <returns></returns>
    public Vector2 MousePosition()
    {
      return ScreenToCanvasSpace(Event.current.mousePosition);
    }

    /// <summary>
    /// Tests if the rect is under the mouse.
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public bool IsUnderMouse(Rect r)
    {
      return r.Contains(MousePosition());
    }

    /// <summary>
    /// Converts the canvas position to screen space.
    /// This only works for geometry inside the ScaleUtility.BeginScale()
    /// </summary>
    /// <param name="canvasPos"></param>
    /// <returns></returns>
    public Vector2 CanvasToScreenSpace(Vector2 canvasPos)
    {
      return (0.5f * window.CanvasRect.size * canvas.ZoomScale) + canvas.panOffset + canvasPos;
    }

    /// <summary>
    /// Convertes the screen position to canvas space.
    /// </summary>
    public Vector2 ScreenToCanvasSpace(Vector2 screenPos)
    {
      return (screenPos - 0.5f * window.CanvasRect.size) * canvas.ZoomScale - canvas.panOffset;
    }

    /// <summary>
    /// Converts the canvas position to screen space.
    /// This works for geometry NOT inside the ScaleUtility.BeginScale().
    /// </summary>
    /// <param name="canvasPos"></param>
    //public void CanvasToScreenSpaceZoomAdj(ref Vector2 canvasPos)
    //{
    //  canvasPos = CanvasToScreenSpace(canvasPos) / canvas.ZoomScale;
    //}

    /// <summary>
    /// Executes the callback on the first node that is detected under the mouse.
    /// </summary>
    /// <param name="callback"></param>
    public bool OnMouseOverNode(Action<BonsaiNode> callback)
    {
      foreach (var node in canvas)
      {

        if (IsUnderMouse(node.bodyRect))
        {
          callback(node);
          return true;
        }
      }

      // No node under mouse.
      return false;
    }

    /// <summary>
    /// Tests if the mouse is over an output.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool OnMouseOverOutput(Action<BonsaiOutputKnob> callback)
    {
      foreach (var node in canvas)
      {

        if (node.Output == null)
        {
          continue;
        }

        if (IsUnderMouse(node.Output.bodyRect))
        {
          callback(node.Output);
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Tests if the mouse is over an input.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool OnMouseOverInput(Action<BonsaiInputKnob> callback)
    {
      foreach (var node in canvas)
      {

        if (node.Input == null)
        {
          continue;
        }

        if (IsUnderMouse(node.Input.bodyRect))
        {
          callback(node.Input);
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Tests if the mouse is over the node or the input.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool OnMouseOverNode_OrInput(Action<BonsaiNode> callback)
    {
      foreach (var node in canvas)
      {

        bool bCondition = IsUnderMouse(node.bodyRect) ||
            (node.Input != null && IsUnderMouse(node.Input.bodyRect));

        if (bCondition)
        {
          callback(node);
          return true;
        }
      }

      // No node under mouse.
      return false;
    }

    private void handleNewNodeToPositionUnderMouse()
    {
      if (_nodeToPositionUnderMouse != null)
      {
        _nodeToPositionUnderMouse.bodyRect.position = MousePosition();
        _nodeToPositionUnderMouse = null;
      }
    }

    internal void SetNewNodeToPositionUnderMouse(BonsaiNode node)
    {
      _nodeToPositionUnderMouse = node;
    }

    #endregion

    #region Node Construction

    /// <summary>
    /// Constructs the nodes for the first time.
    /// This should be used when a canvas is first loaded up
    /// to visualize the tree.
    /// </summary>
    public void ConstructNodesFromTree()
    {
      var nodeMap = reconstructEditorNodes();
      reconstructEditorConnections(nodeMap);
    }

    /// <summary>
    /// Formats the tree to look nicely.
    /// </summary>
    public void PositionNodesNicely()
    {
      var bt = window.tree;

      // Assumption for Nicify:
      // There must be a root set.
      if (bt.Root == null)
      {
        return;
      }

      // This is for the node editor to use to place the nodes.
      var positions = new Dictionary<BehaviourNode, Vector2>();
      var levels = calculateLevels();
      var posParams = new PositioningParameters();

      Action<BehaviourNode> positionInPlace = (node) =>
      {
        positionNode(node, positions, levels, posParams);
      };

      TreeIterator<BehaviourNode>.Traverse(bt.Root, positionInPlace, Traversal.PostOrder);

      foreach (var editorNode in canvas.Nodes)
      {

        var behaviour = editorNode.behaviour;

        if (positions.ContainsKey(behaviour))
        {

          Vector2 pos = positions[behaviour];
          editorNode.bodyRect.position = pos;
        }
      }
    }

    // Reconstruct editor nodes from the tree.
    private Dictionary<BehaviourNode, BonsaiNode> reconstructEditorNodes()
    {
      var nodeMap = new Dictionary<BehaviourNode, BonsaiNode>();

      foreach (var behaviour in window.tree.AllNodes)
      {

        var editorNode = canvas.CreateNode(behaviour);
        editorNode.behaviour = behaviour;
        editorNode.bodyRect.position = behaviour.bonsaiNodePosition;

        nodeMap.Add(behaviour, editorNode);
      }

      return nodeMap;
    }

    // Reconstruct the editor connections from the tree.
    private void reconstructEditorConnections(Dictionary<BehaviourNode, BonsaiNode> nodeMap)
    {
      // Create the connections
      foreach (var bonsaiNode in canvas.Nodes)
      {

        for (int i = 0; i < bonsaiNode.behaviour.ChildCount(); ++i)
        {

          BehaviourNode child = bonsaiNode.behaviour.GetChildAt(i);
          BonsaiInputKnob input = nodeMap[child].Input;

          bonsaiNode.Output.Add(input);
        }
      }
    }

    private void positionNode(
        BehaviourNode node,
        Dictionary<BehaviourNode, Vector2> positions,
        Dictionary<BehaviourNode, int> levels,
        PositioningParameters posParams
        )
    {
      // Obtained from level order of tree.
      float yLevel = levels[node] * posParams.yLevelOffset;

      int childCount = node.ChildCount();

      // If it is a parent of 2 or more children then center in between the children.
      if (childCount > 1)
      {

        BehaviourNode firstChild = node.GetChildAt(0);
        BehaviourNode lastChild = node.GetChildAt(childCount - 1);

        // Get the x-midpoint between the first and last children.
        Vector2 firstChildPos = positions[firstChild];
        Vector2 lastChildPos = positions[lastChild];

        float xMid = (firstChildPos.x + lastChildPos.x) / 2f;
        posParams.xIntermediate = xMid;

        positions.Add(node, new Vector2(xMid, yLevel));
      }

      // A node with 1 child
      else if (childCount == 1)
      {
        positions.Add(node, new Vector2(posParams.xIntermediate, yLevel));
      }

      // A leaf node
      else
      {

        Vector2 position = new Vector2(posParams.xLeaf, yLevel);

        posParams.xIntermediate = posParams.xLeaf;

        float width = calculateNameWidth(node);

        // Offset the x leaf position for the next leaf node.
        if (width > BonsaiNode.kDefaultSize.x)
        {
          posParams.xLeaf += width + PositioningParameters.xPadding;
        }

        else
        {
          posParams.xLeaf += posParams.xDeltaLeaf;
        }

        positions.Add(node, position);
      }
    }

    // Does a quick calculation to see how many pixels the type name takes up.
    private static float calculateNameWidth(BehaviourNode node)
    {
      string typename = node.GetType().Name;
      string niceName = ObjectNames.NicifyVariableName(typename);

      var content = new GUIContent(niceName);
      Vector2 size = new GUIStyle().CalcSize(content);

      return size.x + BonsaiNode.resizePaddingX;
    }

    // To do this, we do a regular DFS and just check the current
    // path length at a given node to determine its level.
    private Dictionary<BehaviourNode, int> calculateLevels()
    {
      var bt = window.tree;

      if (bt.Root == null)
      {
        return null;
      }

      var levels = new Dictionary<BehaviourNode, int>();

      Action<BehaviourNode, TreeIterator<BehaviourNode>> setLevel = (node, itr) =>
      {
        levels.Add(node, itr.CurrentLevel);
      };

      TreeIterator<BehaviourNode>.Traverse(bt.Root, setLevel, Traversal.LevelOrder);

      return levels;
    }

    /// <summary>
    /// A helper class to hold some positioning data when building the canvas.
    /// </summary>
    private class PositioningParameters
    {
      public float xLeaf = 0f;

      // This is used for single child nodes.
      // That way it is chained in a line.
      public float xIntermediate = 0f;

      public static float xPadding = 30f;
      public static float yPadding = 40f;

      // The displacment between leaves in the x axis.
      public readonly float xDeltaLeaf = BonsaiNode.kDefaultSize.x + xPadding;

      // Displacment between nodes in the y axis.
      public readonly float yLevelOffset = BonsaiNode.kDefaultSize.y + yPadding;
    }
    #endregion

    #region Content and Styles

    private GUIStyle _modeStatusStyle;

    private Rect _modeStatusRect = new Rect(20f, 20f, 250f, 150f);

    private GUIStyle ModeStatusStyle
    {
      get
      {
        if (_modeStatusStyle == null)
        {
          _modeStatusStyle = new GUIStyle();
          _modeStatusStyle.fontSize = 36;
          _modeStatusStyle.fontStyle = FontStyle.Bold;
          _modeStatusStyle.normal.textColor = new Color(1f, 1f, 1f, 0.2f);
        }

        return _modeStatusStyle;
      }
    }

    #endregion

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

          object[] nodeProperties = type.GetCustomAttributes(typeof(NodeEditorPropertiesAttribute), false);

          // The attribute is to simply get custom data about the node.
          // Like menu path and texture.
          NodeEditorPropertiesAttribute attrib = null;
          if (nodeProperties.Length > 0)
          {
            attrib = nodeProperties[0] as NodeEditorPropertiesAttribute;
          }

          string menuPath = "Uncategorized/";
          string texName = "Play";

          if (attrib != null)
          {
            menuPath = attrib.menuPath;
            texName = attrib.textureName;
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