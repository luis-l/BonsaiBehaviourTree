
using UnityEngine;

using Bonsai.Designer;
using Bonsai.Core;
using System.Text;

namespace Bonsai.Standard
{
  [BonsaiNode("Tasks/", "Timer")]
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

    public override void StaticDescription(StringBuilder builder)
    {
      builder.AppendFormat("Wait for {0:0.00}s", waitTime);
    }
  }
}