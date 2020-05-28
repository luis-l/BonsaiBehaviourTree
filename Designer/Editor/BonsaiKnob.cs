
using System;
using UnityEngine;

using Bonsai.Core;

namespace Bonsai.Designer
{
  public abstract class BonsaiKnob
  {
    internal Texture2D background;

    public static readonly Vector2 kMinSize = new Vector2(40, 10f);

    /// <summary>
    /// The rect defining the area of the knob in canvas space.
    /// </summary>
    public Rect bodyRect = new Rect(Vector2.zero, kMinSize);

    /// <summary>
    /// The node that the knob belongs to.
    /// </summary>
    internal BonsaiNode parentNode;
  }
}