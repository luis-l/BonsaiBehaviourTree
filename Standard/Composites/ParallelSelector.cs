
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
  [NodeEditorProperties("Composites/", "ParallelQuestion")]
  public class ParallelSelector : Parallel
  {
    public override Status Run()
    {
      // All iterators done.
      // Since there was no success interruption, that means
      // all iterators returned failure, so the parallel node
      // returns failure aswell.
      if (IsDone)
      {
        return Status.Failure;
      }

      // Process the sub-iterators.
      for (int i = 0; i < _subIterators.Count; ++i)
      {

        BehaviourIterator itr = _subIterators[i];

        // Keep updating the iterators that are not done.
        if (itr.IsRunning)
        {
          itr.Update();
        }

        // Iterator finished, it must have returned Success or Failure.
        // If the iterator returned success, then interrupt the parallel process
        // and have the parallel node return success.
        else if (itr.LastStatusReturned == Status.Success)
        {
          return Status.Success;
        }
      }

      // Parallel iterators still running.
      return Status.Running;
    }
  }
}