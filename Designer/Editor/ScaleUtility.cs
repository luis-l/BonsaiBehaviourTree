using UnityEngine;

namespace Bonsai.Designer
{
  public static class ScaleUtility
  {
    // Helper method to compute the scaled Rect position for BeingScale and EndScale.
    private static float ScaledCoord(float zoom, float dimension)
    {
      return 0.5f * ((dimension * zoom) - dimension);
    }

    public static void BeginScale(Rect rect, float scale, float topPadding)
    {
      GUI.EndClip();
      GUIUtility.ScaleAroundPivot(Vector2.one / scale, rect.size * 0.5f);

      var position = new Vector2(-ScaledCoord(scale, rect.width), -ScaledCoord(scale, rect.height) + (topPadding * scale));
      var size = new Vector2(rect.width * scale, rect.height * scale);

      GUI.BeginClip(new Rect(position, size));
    }

    public static void EndScale(Rect rect, float scale, float topPadding)
    {
      GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.size * 0.5f);

      var offset = new Vector3(
          ScaledCoord(scale, rect.width),
          ScaledCoord(scale, rect.height) + (-topPadding * scale) + topPadding,
          0);

      GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
    }
  }
}