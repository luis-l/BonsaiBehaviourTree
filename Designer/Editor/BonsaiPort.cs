
using UnityEngine;

namespace Bonsai.Designer
{
  public abstract class BonsaiPort
  {
    public static readonly Vector2 kMinSize = new Vector2(40f, 15f);

    /// <summary>
    /// The rect defining the area of the port in canvas space.
    /// </summary>
    public Rect bodyRect = new Rect(Vector2.zero, kMinSize);

    /// <summary>
    /// The node that the port belongs to.
    /// </summary>
    internal BonsaiNode parentNode;
  }
}