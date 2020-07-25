
using System.Text;
using Bonsai.Core;

namespace Bonsai.Standard
{
  [BonsaiNode("Decorators/", "RepeatArrow")]
  public sealed class Repeater : Decorator
  {
    public int loopCount = 1;
    public bool infiniteLoop = false;

    private int loopCounter = 0;

    public override void OnEnter()
    {
      loopCounter = 0;
    }

    public override Status Run()
    {
      // Infinite loop always returns running and always traverses the child.
      if (infiniteLoop)
      {
        Iterator.Traverse(child);
        return Status.Running;
      }

      else
      {
        // If we have not exceeded the loop count then traverse the child.
        if (loopCounter < loopCount)
        {
          loopCounter++;
          Iterator.Traverse(child);
          return Status.Running;
        }

        // Finished looping, return what the child returns.
        else
        {
          return Iterator.LastChildExitStatus.GetValueOrDefault(Status.Failure);
        }
      }
    }

    public override void Description(StringBuilder builder)
    {
      if (infiniteLoop)
      {
        builder.Append("Loop forever");
      }
      else if (loopCount < 1)
      {
        builder.Append("Don't loop");
      }
      else if (loopCount > 1)
      {
        builder.AppendFormat("Loop {0} times", loopCount);
      }
      else
      {
        builder.Append("Loop once");
      }
    }

  }
}