
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Input/", "Mouse")]
  public class IsMouseButtonUp : ConditionalAbort
  {
    public int button = 0;

    public override bool Condition()
    {
      return Input.GetMouseButtonUp(button);
    }
  }
}