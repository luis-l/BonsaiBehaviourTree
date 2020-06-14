using UnityEngine;

namespace Bonsai.Utility
{
  public static class MathExtensions
  {
    public static Vector2 Round(Vector2 v)
    {
      return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    }

    public static Rect Round(Rect r)
    {
      return new Rect(Round(r.position), Round(r.size));
    }
  }

}
