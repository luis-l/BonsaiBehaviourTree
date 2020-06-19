using System;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace Bonsai.Designer
{

  /// <summary>
  /// Provides utilities to do grid transformations and checks.
  /// </summary>
  public class Coord
  {
    /// <summary>
    /// The canvas used for coordinate transformations and checks.
    /// </summary>
    private readonly BonsaiCanvas canvas;

    /// <summary>
    /// The window used for coordinate transformations and checks.
    /// </summary>
    private readonly BonsaiWindow window;

    public Coord(BonsaiCanvas canvas, BonsaiWindow window)
    {
      this.canvas = canvas;
      this.window = window;
    }

    /// <summary>
    /// Tests if the node is in view of the window.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    [Pure]
    public bool IsInView(BonsaiNode node)
    {
      var nodeRect = new Rect(CanvasToScreenSpace(node.Position), node.Size);
      Rect viewRect = window.CanvasRect;
      viewRect.size *= canvas.ZoomScale;
      return viewRect.Overlaps(nodeRect);
    }

    /// <summary>
    /// Test if the line segment is in viewe of the window. 
    /// Only works for axis aligned lines (horizontal or vertical).
    /// </summary>
    /// <param name="start">Start point of the line in screen space.</param>
    /// <param name="end">End point of the line in screen space</param>
    /// <returns></returns>
    [Pure]
    public bool IsScreenAxisLineInView(Vector2 start, Vector2 end)
    {
      var lineBox = new Rect { position = start, max = end };
      Rect viewRect = window.CanvasRect;
      viewRect.size *= canvas.ZoomScale;
      return viewRect.Overlaps(lineBox);
    }

    /// <summary>
    /// Converts the canvas position to screen space.
    /// This only works for geometry inside the ScaleUtility.BeginScale()
    /// </summary>
    /// <param name="canvasPos"></param>
    /// <returns></returns>
    [Pure]
    public Vector2 CanvasToScreenSpace(Vector2 canvasPos)
    {
      return (0.5f * window.CanvasRect.size * canvas.ZoomScale) + canvas.panOffset + canvasPos;
    }

    /// <summary>
    /// Convertes the screen position to canvas space.
    /// </summary>
    [Pure]
    public Vector2 ScreenToCanvasSpace(Vector2 screenPos)
    {
      return (screenPos - 0.5f * window.CanvasRect.size) * canvas.ZoomScale - canvas.panOffset;
    }

    /// <summary>
    /// Converts the canvas position to screen space.
    /// This works for geometry NOT inside the ScaleUtility.BeginScale().
    /// </summary>
    /// <param name="canvasPos"></param>
    //[Pure]
    //public void CanvasToScreenSpaceZoomAdj(ref Vector2 canvasPos)
    //{
    //  canvasPos = CanvasToScreenSpace(canvasPos) / canvas.ZoomScale;
    //}

    /// <summary>
    /// Rounds the position to the nearest grid coordinate.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [Pure]
    public static Vector2 SnapPosition(Vector2 p, float snapStep)
    {
      return SnapPosition(p.x, p.y, snapStep);
    }

    /// <summary>
    /// Rounds the position to the nearest grid coordinate.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [Pure]
    public static Vector2 SnapPosition(float x, float y, float snapStep)
    {
      x = Mathf.Round(x / snapStep) * snapStep;
      y = Mathf.Round(y / snapStep) * snapStep;

      return new Vector2(x, y);
    }

    /// <summary>
    /// Returns the mouse position in canvas space.
    /// </summary>
    /// <returns></returns>
    [Pure]
    public Vector2 MousePosition()
    {
      return ScreenToCanvasSpace(Event.current.mousePosition);
    }

    /// <summary>
    /// Tests if the rect is under the mouse.
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    [Pure]
    public bool IsUnderMouse(Rect r)
    {
      return r.Contains(MousePosition());
    }

    /// <summary>
    /// Get the first node detected under the mouse.  
    /// Ports are not counted as part of the node.
    /// </summary>
    /// <returns>Null if there was no node under the mouse.</returns>
    public BonsaiNode NodeUnderMouseExcludePorts()
    {
      foreach (BonsaiNode node in canvas)
      {
        if (IsUnderMouse(node.RectPositon) && !IsMouseOverNodePorts(node))
        {
          return node;
        }
      }

      // No node under mouse.
      return null;
    }

    /// <summary>
    /// Get the first node detected under the mouse. Ports are counted as port of the check.
    /// </summary>
    /// <returns></returns>
    public BonsaiNode NodeUnderMouse()
    {
      foreach (BonsaiNode node in canvas)
      {
        if (IsUnderMouse(node.RectPositon))
        {
          return node;
        }
      }

      // No node under mouse.
      return null;

    }

    public enum MouseQueryResult
    {
      Nothing,
      Node,
      Input,
      Output
    }

    public MouseQueryResult QueryUnderMouse(out BonsaiNode node, out BonsaiInputPort input, out BonsaiOutputPort output)
    {
      input = null;
      output = null;
      node = NodeUnderMouse();

      if (node == null)
      {
        return MouseQueryResult.Nothing;
      }

      input = InputUnderMouse(node);

      if (input != null)
      {
        return MouseQueryResult.Input;
      }

      output = OutputUnderMouse(node);
      if (output != null)
      {
        return MouseQueryResult.Output;
      }

      return MouseQueryResult.Node;
    }

    /// <summary>
    /// Get the input for the node if under the mouse.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public BonsaiInputPort InputUnderMouse(BonsaiNode node)
    {
      if (node.Input != null && IsUnderMouse(node.Input.RectPosition))
      {
        return node.Input;
      }

      return null;
    }

    /// <summary>
    /// Get the ouput for the node if under the mouse.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public BonsaiOutputPort OutputUnderMouse(BonsaiNode node)
    {
      if (node.Output != null && IsUnderMouse(node.Output.RectPosition))
      {
        return node.Output;
      }
      return null;
    }

    /// <summary>
    /// Test if the if the mouse is over any node.
    /// </summary>
    /// <returns></returns>
    public bool IsMouseOverNode()
    {
      foreach (BonsaiNode node in canvas)
      {
        if (IsUnderMouse(node.RectPositon) && !IsMouseOverNodePorts(node))
        {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Executes the callback on the first node that is detected under the mouse.
    /// </summary>
    /// <param name="callback"></param>
    public bool OnMouseOverNode(Action<BonsaiNode> callback)
    {
      foreach (BonsaiNode node in canvas)
      {
        if (IsUnderMouse(node.RectPositon) && !IsMouseOverNodePorts(node))
        {
          callback(node);
          return true;
        }
      }

      // No node under mouse.
      return false;
    }

    /// <summary>
    /// Get the first ouptut port detected under the mouse.
    /// </summary>
    /// <returns>null if nothing is found</returns>
    public BonsaiOutputPort OutputUnderMouse()
    {
      foreach (BonsaiNode node in canvas)
      {
        if (node.Output == null)
        {
          continue;
        }

        if (IsUnderMouse(node.Output.RectPosition))
        {
          return node.Output;
        }
      }

      return null;
    }

    /// <summary>
    /// Get the first input port detected under the mouse.
    /// </summary>
    /// <returns>null if nothing is found</returns>
    public BonsaiInputPort InputUnderMouse()
    {
      foreach (BonsaiNode node in canvas)
      {
        if (node.Input == null)
        {
          continue;
        }

        if (IsUnderMouse(node.Input.RectPosition))
        {
          return node.Input;
        }
      }
      return null;
    }

    /// <summary>
    /// Test if the mouse in over the input or output ports for the node.
    /// </summary>
    /// <returns></returns>
    [Pure]
    public bool IsMouseOverNodePorts(BonsaiNode node)
    {
      if (node.Output != null && IsUnderMouse(node.Output.RectPosition))
      {
        return true;
      }

      if (node.Input != null && IsUnderMouse(node.Input.RectPosition))
      {
        return true;
      }

      return false;
    }

    /// <summary>
    /// Tests if the mouse is over an output.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool OnMouseOverOutput(Action<BonsaiOutputPort> callback)
    {
      BonsaiOutputPort port = OutputUnderMouse();
      if (port != null)
      {
        callback(port);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tests if the mouse is over an input.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool OnMouseOverInput(Action<BonsaiInputPort> callback)
    {
      BonsaiInputPort port = InputUnderMouse();
      if (port != null)
      {
        callback(port);
        return true;
      }
      return false;
    }

    /// <summary>
    /// Tests if the mouse is over the node or the input.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public bool OnMouseOverNodeOrInput(Action<BonsaiNode> callback)
    {
      foreach (BonsaiNode node in canvas)
      {
        bool bCondition = IsUnderMouse(node.RectPositon)
          || (node.Input != null && IsUnderMouse(node.Input.RectPosition));

        if (bCondition)
        {
          callback(node);
          return true;
        }
      }

      // No node under mouse.
      return false;
    }

  }

}
