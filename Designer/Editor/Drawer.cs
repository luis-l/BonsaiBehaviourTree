
using UnityEngine;

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
  }

}