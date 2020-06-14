
using System.IO.Compression;
using UnityEngine;

namespace Bonsai.Designer
{
  public abstract class BonsaiPort
  {
    public BonsaiPort(BonsaiNode node)
    {
      ParentNode = node;
    }

    /// <summary>
    /// The rect defining the area of the port in canvas space.
    /// </summary>
    public Rect bodyRect = new Rect(Vector2.zero, new Vector2(1f, BonsaiPreferences.Instance.portHeight));

    /// <summary>
    /// The node that the port belongs to.
    /// </summary>
    public BonsaiNode ParentNode { get; private set; }
  }
}