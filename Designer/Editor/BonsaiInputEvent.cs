
using UnityEngine;

namespace Bonsai.Designer
{
  public class BonsaiInputEvent
  {
    public CanvasTransform transform;
    public Vector2 canvasMousePostion;
    public BonsaiNode node;
    public BonsaiInputPort inputPort;
    public BonsaiOutputPort outputPort;

    public bool IsPortFocused()
    {
      return inputPort != null || outputPort != null;
    }

    public bool IsNodeFocused()
    {
      return node != null;
    }
  }
}
