
using System.Linq;
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Composites/", "ParallelQuestion")]
  public class ParallelSelector : Parallel
  {
    public override Status Run()
    {
      if (IsAnyChildWithStatus(Status.Success))
      {
        return Status.Success;
      }

      if (AreAllChildrenWithStatus(Status.Failure))
      {
        return Status.Failure;
      }

      // Process the sub-iterators.
      for (int i = 0; i < _subIterators.Count; ++i)
      {
        // Keep updating the iterators that are not done.
        BehaviourIterator itr = _subIterators[i];
        if (itr.IsRunning)
        {
          itr.Update();
        }
      }

      // Parallel iterators still running.
      return Status.Running;
    }
  }
}