
using UnityEngine;

using Bonsai.Designer;
using Bonsai.Core;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Tasks/", "Timer")]
  public class Wait : Task
  {
    private float _timer = 0f;

    public float waitTime = 1f;

    public override void OnEnter()
    {
      _timer = 0f;
    }

    public override Status Run()
    {
      _timer += Time.deltaTime;

      if (_timer >= waitTime)
      {
        return Status.Success;
      }

      return Status.Running;
    }
  }
}