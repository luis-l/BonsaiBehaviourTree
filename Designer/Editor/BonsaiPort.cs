
using UnityEngine;

namespace Bonsai.Designer
{
  public abstract class BonsaiPort
  {
    public BonsaiPort(BonsaiNode node)
    {
      ParentNode = node;
    }

    public Rect RectPosition { get; set; }

    /// <summary>
    /// The node that the port belongs to.
    /// </summary>
    public BonsaiNode ParentNode { get; }
  }
}