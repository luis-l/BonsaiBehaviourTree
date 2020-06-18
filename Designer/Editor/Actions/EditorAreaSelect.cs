
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonsai.Designer
{
  public class EditorAreaSelect
  {
    public Vector2 StartPosition { get; private set; }

    public bool IsSelecting { get; private set; }

    public void BeginAreaSelection(Vector2 start)
    {
      StartPosition = start;
      IsSelecting = true;
    }

    public void EndAreaSelection()
    {
      IsSelecting = false;
    }

    public IEnumerable<BonsaiNode> NodesUnderSelection(
      Coord c,
      Vector2 endPosition,
      IEnumerable<BonsaiNode> allNodes)
    {
      if (IsSelecting)
      {
        Rect selectionArea = SelectionCanvasSpace(c, endPosition);
        return allNodes.Where(node => selectionArea.Overlaps(node.RectPositon));
      }

      return Enumerable.Empty<BonsaiNode>();
    }

    /// <summary>
    /// Returns the area selection in screen space.
    /// </summary>
    /// <returns></returns>
    public Rect SelectionScreenSpace(Vector2 endPosition)
    {
      // Need to find the proper min and max values to 
      // create a rect without negative width/height values.
      float xmin, xmax;
      float ymin, ymax;

      if (StartPosition.x < endPosition.x)
      {
        xmin = StartPosition.x;
        xmax = endPosition.x;
      }

      else
      {
        xmax = StartPosition.x;
        xmin = endPosition.x;
      }

      if (StartPosition.y < endPosition.y)
      {
        ymin = StartPosition.y;
        ymax = endPosition.y;
      }

      else
      {
        ymax = StartPosition.y;
        ymin = endPosition.y;
      }

      return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
    }

    /// <summary>
    /// Returns the selection rect in canvas space.
    /// </summary>
    /// <returns></returns>
    public Rect SelectionCanvasSpace(Coord c, Vector2 endPosition)
    {
      Rect screenRect = SelectionScreenSpace(endPosition);
      Vector2 min = c.ScreenToCanvasSpace(screenRect.min);
      Vector2 max = c.ScreenToCanvasSpace(screenRect.max);
      return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

  }
}
