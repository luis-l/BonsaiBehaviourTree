
using UnityEngine;

namespace Bonsai.Designer
{
  public struct CanvasTransform
  {
    /// <summary>
    /// The canvas offset (translation).
    /// </summary>
    public Vector2 pan;

    /// <summary>
    /// The canvas zoom scale.
    /// </summary>
    public float zoom;

    /// <summary>
    /// Width and height of the canvas.
    /// </summary>
    public Vector2 size;

    /// <summary>
    /// Tests if the a rect in canvas space is in view.
    /// </summary>
    /// <param name="rectInCanvasSpace">The rect in canvas space.</param>
    /// <returns>The result of the overlap test</returns>
    public bool InView(Rect rectInCanvasSpace)
    {
      var rectInScreenSpace = new Rect(CanvasToScreenSpace(rectInCanvasSpace.position), rectInCanvasSpace.size);
      var viewRect = new Rect(Vector2.zero, size * zoom);
      return viewRect.Overlaps(rectInScreenSpace);
    }

    /// <summary>
    /// Test if the line segment is in viewe of the window. 
    /// Only works for axis aligned lines (horizontal or vertical).
    /// </summary>
    /// <param name="start">Start point of the line in screen space.</param>
    /// <param name="end">End point of the line in screen space</param>
    /// <returns></returns>
    public bool IsScreenAxisLineInView(Vector2 start, Vector2 end)
    {
      var lineBox = new Rect { position = start, max = end };
      Rect viewRect = new Rect(Vector2.zero, size * zoom);
      return viewRect.Overlaps(lineBox);
    }

    /// <summary>
    /// Converts the canvas position to screen space.
    /// This only works for geometry inside the ScaleUtility.BeginScale()
    /// </summary>
    /// <param name="canvasPosition"></param>
    /// <returns></returns>
    public Vector2 CanvasToScreenSpace(Vector2 canvasPosition)
    {
      return (0.5f * size * zoom) + pan + canvasPosition;
    }

    /// <summary>
    /// Convertes the screen position to canvas space.
    /// </summary>
    public Vector2 ScreenToCanvasSpace(Vector2 screenPosition)
    {
      return (screenPosition - 0.5f * size) * zoom - pan;
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

  }
}
