
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Input/", "Keyboard")]
  public class IsKeyUp : ConditionalAbort
  {
    public KeyCode key;

    public override bool Condition()
    {
      return Input.GetKeyUp(key);
    }
  }
}