
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

  }

}