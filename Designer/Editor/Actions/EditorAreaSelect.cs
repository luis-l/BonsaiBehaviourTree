
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonsai.Designer
{
  public static class EditorAreaSelect
  {
    /// <summary>
    /// Returns the nodes under the area.
    /// </summary>
    /// <param name="coord"></param>
    /// <param name="nodes"></param>
    /// <param name="startScreenSpace"></param>
    /// <param name="endScreenSpace"></param>
    /// <returns></returns>
    public static IEnumerable<BonsaiNode> NodesUnderArea(
      Coord coord,
      IEnumerable<BonsaiNode> nodes,
      Vector2 startScreenSpace,
      Vector2 endScreenSpace)
    {
      Rect selectionArea = SelectionCanvasSpace(coord, startScreenSpace, endScreenSpace);
      return nodes.Where(node => selectionArea.Overlaps(node.RectPositon));
    }

    /// <summary>
    /// Returns the area selection in screen space.
    /// </summary>
    /// <returns></returns>
    public static Rect SelectionScreenSpace(Vector2 start, Vector2 end)
    {
      // Need to find the proper min and max values to 
      // create a rect without negative width/height values.
      float xmin, xmax;
      float ymin, ymax;

      if (start.x < end.x)
      {
        xmin = start.x;
        xmax = end.x;
      }

      else
      {
        xmax = start.x;
        xmin = end.x;
      }

      if (start.y < end.y)
      {
        ymin = start.y;
        ymax = end.y;
      }

      else
      {
        ymax = start.y;
        ymin = end.y;
      }

      return Rect.MinMaxRect(xmin, ymin, xmax, ymax);
    }

    /// <summary>
    /// Returns the selection rect in canvas space.
    /// </summary>
    /// <returns></returns>
    public static Rect SelectionCanvasSpace(Coord c, Vector2 start, Vector2 end)
    {
      Rect screenRect = SelectionScreenSpace(start, end);
      Vector2 min = c.ScreenToCanvasSpace(screenRect.min);
      Vector2 max = c.ScreenToCanvasSpace(screenRect.max);
      return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

  }
}
