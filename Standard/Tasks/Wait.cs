
using System.Text;
using Bonsai.Core;
using Bonsai.Designer;
using UnityEngine;

namespace Bonsai.Standard
{
  [BonsaiNode("Tasks/", "Timer")]
  public class Wait : Task
  {
    [ShowAtRuntime]
    private float timer = 0f;

    public float waitTime = 1f;

    public override void OnEnter()
    {
      timer = 0f;
    }

    public override Status Run()
    {
      timer += Time.deltaTime;

      if (timer >= waitTime)
      {
        return Status.Success;
      }

      return Status.Running;
    }

    public override void Description(StringBuilder builder)
    {
      builder.AppendFormat("Wait for {0:0.00}s", waitTime);
    }
  }
}