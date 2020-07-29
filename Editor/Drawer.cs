
using UnityEngine;
using UnityEditor;

namespace Bonsai.Designer
{
  /// <summary>
  /// Provides utilities to draw elements in the editor.
  /// </summary>
  public static class Drawer
  {

    /// <summary>
    /// Draws a static grid that is unaffected by zoom and pan.
    /// </summary>
    /// <param name="canvas">The area to draw the grid</param>
    /// <param name="texture">The grid tile texture</param>
    public static void DrawStaticGrid(Rect canvas, Texture2D texture)
    {
      var size = canvas.size;
      var center = size / 2f;

      float xOffset = -center.x / texture.width;
      float yOffset = (center.y - size.y) / texture.height;

      // Offset from origin in tile units
      Vector2 tileOffset = new Vector2(xOffset, yOffset);

      float tileAmountX = Mathf.Round(size.x) / texture.width;
      float tileAmountY = Mathf.Round(size.y) / texture.height;

      // Amount of tiles
      Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

      // Draw tiled background
      GUI.DrawTextureWithTexCoords(canvas, texture, new Rect(tileOffset, tileAmount));
    }

    /// <summary>
    /// Draw a tiled grid that can be scaled and translated.
    /// </summary>
    /// <param name="canvas">The area to draw the grid</param>
    /// <param name="texture">The grid tile texture</param>
    /// <param name="zoom">Scales the grid by zoom amount</param>
    /// <param name="pan">Translates the grid pan amount</param>
    public static void DrawGrid(Rect canvas, Texture texture, float zoom, Vector2 pan)
    {
      var size = canvas.size;
      var center = size / 2f;

      // Offset from origin in tile units
      float xOffset = -(center.x * zoom + pan.x) / texture.width;
      float yOffset = ((center.y - size.y) * zoom + pan.y) / texture.height;

      Vector2 tileOffset = new Vector2(xOffset, yOffset);

      // Amount of tiles
      float tileAmountX = Mathf.Round(size.x * zoom) / texture.width;
      float tileAmountY = Mathf.Round(size.y * zoom) / texture.height;

      Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

      // Draw tiled background
      GUI.DrawTextureWithTexCoords(canvas, texture, new Rect(tileOffset, tileAmount));
    }

    public static void DrawNode(
      CanvasTransform t,
      BonsaiNode node,
      Color statusColor)
    {
      // Convert the node rect from canvas to screen space.
      Rect screenRect = node.RectPositon;
      screenRect.position = t.CanvasToScreenSpace(screenRect.position);

      // Remember the original color that way it is reset when the function exits.
      Color originalColor = GUI.color;

      DrawNodeBackground(screenRect, statusColor);

      // The node contents are grouped together within the node body.
      GUI.BeginGroup(screenRect);

      // Make the body of node local to the group coordinate space.
      Rect localRect = node.RectPositon;
      localRect.position = Vector2.zero;

      // Draw the contents inside the node body, automatically laid out.
      GUILayout.BeginArea(localRect, GUIStyle.none);

      DrawNodeTypeBackground(node);
      DrawExitStatus(node);
      DrawNodeContent(node);

      GUILayout.EndArea();

      GUI.EndGroup();
      GUI.color = originalColor;
    }

    public static void DrawNodeBackground(Rect screenRect, Color color)
    {
      GUI.DrawTexture(
        screenRect, BonsaiPreferences.Instance.nodeBackgroundTexture,
        ScaleMode.StretchToFill,
        true,
        0,
        color,
        0,
        5f);
    }

    // Render the background color scheme for the node type.
    public static void DrawNodeTypeBackground(BonsaiNode node)
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

    private static Color NodeTypeColor(BonsaiNode node)
    {
      if (node.Behaviour is Core.Task)
      {
        return BonsaiPreferences.Instance.taskColor;
      }

      else if (node.Behaviour is Core.Service)
      {
        return BonsaiPreferences.Instance.serviceColor;
      }

      else if (node.Behaviour is Core.ConditionalAbort)
      {
        return BonsaiPreferences.Instance.conditionalColor;
      }

      else if (node.Behaviour is Core.Decorator)
      {
        return BonsaiPreferences.Instance.decoratorColor;
      }

      return BonsaiPreferences.Instance.compositeColor;
    }

    // Render the node body contents.
    private static void DrawNodeContent(BonsaiNode node)
    {
      GUILayout.Box(node.HeaderContent, node.HeaderStyle);
      GUILayout.Box(node.BodyContent, node.BodyStyle);
    }

    public static void DrawExitStatus(BonsaiNode node)
    {
      // Draw the exit status in the top right corner.
      float statusSize = BonsaiPreferences.Instance.statusIconSize;
      Rect contentRect = node.ContentRect;
      var rect = new Rect(contentRect.xMax - statusSize, contentRect.yMin, statusSize, statusSize);

      var prefs = BonsaiPreferences.Instance;
      var status = node.Behaviour.StatusEditorResult;

      if (status == Core.BehaviourNode.StatusEditor.Success)
      {
        DrawTexture(rect, prefs.successSymbol, prefs.successColor);
      }

      else if (status == Core.BehaviourNode.StatusEditor.Failure)
      {
        DrawTexture(rect, prefs.failureSymbol, prefs.failureColor);
      }

      else if (status == Core.BehaviourNode.StatusEditor.Aborted)
      {
        DrawTexture(rect, prefs.failureSymbol, prefs.abortedColor);
      }

      else if (status == Core.BehaviourNode.StatusEditor.Interruption)
      {
        DrawTexture(rect, prefs.failureSymbol, prefs.interruptedColor);
      }
    }

    public static void DrawPorts(CanvasTransform t, BonsaiNode node)
    {
      // There is always an input port.
      DrawPort(t, node.InputRect);

      if (node.HasOutput)
      {
        DrawPort(t, node.OutputRect);
      }
    }

    public static void DrawPort(CanvasTransform t, Rect portRect)
    {
      // Convert the body rect from canvas to screen space.
      portRect.position = t.CanvasToScreenSpace(portRect.position);
      GUI.DrawTexture(portRect, BonsaiPreferences.Instance.portTexture, ScaleMode.StretchToFill);
    }

    public static void DrawNodeConnections(CanvasTransform t, BonsaiNode node)
    {
      if (node.ChildCount() == 0)
      {
        return;
      }

      var prefs = BonsaiPreferences.Instance;

      Color connectionColor = prefs.defaultConnectionColor;
      float connectionWidth = prefs.defaultConnectionWidth;

      if (node.Behaviour.StatusEditorResult == Core.BehaviourNode.StatusEditor.Running)
      {
        connectionColor = prefs.runningStatusColor;
        connectionWidth = prefs.runningConnectionWidth;
      }

      // Start the Y anchor coord at the tip of the Output port.
      float yoffset = node.RectPositon.yMax;

      // Calculate the anchor position.
      float anchorX = node.RectPositon.center.x;
      float anchorY = (yoffset + node.GetNearestInputY()) / 2f;

      // Anchor line, between the first and last child.

      // Find the min and max X coords between the children and the parent.
      node.GetBoundsX(out float anchorLineStartX, out float anchorLineEndX);

      // Get start and end positions of the anchor line (The common line where the parent and children connect).
      var anchorLineStart = new Vector2(anchorLineStartX, anchorY);
      var anchorLineEnd = new Vector2(anchorLineEndX, anchorY);

      // The tip where the parent starts its line to connect to the anchor line.
      var parentAnchorTip = new Vector2(anchorX, yoffset);

      // The point where the parent connects to the anchor line.
      var parentAnchorLineConnection = new Vector2(anchorX, anchorY);

      // Draw the lines from the calculated positions.
      DrawLineCanvasSpace(
        t,
        parentAnchorTip,
        parentAnchorLineConnection,
        connectionColor,
        connectionWidth);

      DrawLineCanvasSpace(
        t,
        anchorLineStart,
        anchorLineEnd,
        prefs.defaultConnectionColor,
        prefs.defaultConnectionWidth);

      foreach (BonsaiNode child in node.Children)
      {
        // Get the positions to draw a line between the node and the anchor line.
        Vector2 center = child.InputRect.center;
        var anchorLineConnection = new Vector2(center.x, anchorY);

        // The node is running, hightlight the connection.
        if (child.Behaviour.StatusEditorResult == Core.BehaviourNode.StatusEditor.Running)
        {
          DrawLineCanvasSpace(
            t,
            center,
            anchorLineConnection,
            prefs.runningStatusColor,
            prefs.runningConnectionWidth);

          // Hightlight the portion of the anchorline between the running child and parent node.
          DrawLineCanvasSpace(
            t,
            anchorLineConnection,
            parentAnchorLineConnection,
            prefs.runningStatusColor,
            prefs.runningConnectionWidth);
        }
        else
        {
          // The node is not running, draw a default connection.
          DrawLineCanvasSpace(
            t,
            anchorLineConnection,
            center,
            prefs.defaultConnectionColor,
            prefs.defaultConnectionWidth);
        }
      }
    }

    /// <summary>
    /// Handles drawing a rect line between two points in screen space.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public static void DrawRectConnectionScreenSpace(Vector2 start, Vector2 end, Color color)
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

    /// <summary>
    /// Handles drawing a rect line between two points in canvas space.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    //public static void DrawRectConnectionCanvasSpace(Vector2 start, Vector2 end, Color color)
    //{
    //  start = Coordinates.CanvasToScreenSpace(start);
    //  end = Coordinates.CanvasToScreenSpace(end);
    //  DrawRectConnectionScreenSpace(start, end, color);
    //}

    // Helper method to draw textures with color tint.
    public static void DrawTexture(Rect r, Texture2D tex, Color c)
    {
      GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true, 0f, c, 0f, 0f);
    }

    public static void DrawLineCanvasSpace(CanvasTransform t, Vector2 start, Vector2 end, Color color)
    {
      start = t.CanvasToScreenSpace(start);
      end = t.CanvasToScreenSpace(end);
      DrawLineScreenSpace(start, end, color);
    }

    public static void DrawLineCanvasSpace(CanvasTransform t, Vector2 start, Vector2 end, Color color, float width)
    {
      start = t.CanvasToScreenSpace(start);
      end = t.CanvasToScreenSpace(end);
      if (t.IsScreenAxisLineInView(start, end))
      {
        DrawLineScreenSpace(start, end, color, width);
      }
    }

    public static void DrawLineScreenSpace(Vector2 start, Vector2 end, Color color)
    {
      var originalColor = Handles.color;
      Handles.color = color;
      Handles.DrawLine(start, end);
      Handles.color = originalColor;
    }

    public static void DrawLineScreenSpace(Vector2 start, Vector2 end, Color color, float width)
    {
      var originalColor = Handles.color;
      Handles.color = color;
      Handles.DrawAAPolyLine(width, start, end);
      Handles.color = originalColor;
    }

  }

}