
using UnityEngine;

using Bonsai.Core;
using Bonsai.Designer;

[NodeEditorProperties("Input/", "Keyboard")]
public class IsKeyDown : ConditionalAbort
{

  public KeyCode key;

  public override bool Condition()
  {
    return Input.GetKeyDown(key);
  }
}
