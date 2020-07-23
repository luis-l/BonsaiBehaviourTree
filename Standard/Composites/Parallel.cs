
namespace Bonsai.Standard
{
  /// <summary>
  /// Parallel node which succeeds if all its children succeed.
  /// </summary>
  [BonsaiNode("Composites/", "Parallel")]
  public class Parallel : Core.ParallelComposite
  {
    public override Status Run()
    {
      if (IsAnyChildWithStatus(Status.Failure))
      {
        return Status.Failure;
      }

      if (AreAllChildrenWithStatus(Status.Success))
      {
        return Status.Success;
      }

      // Process the sub-iterators.
      for (int i = 0; i < subIterators.Count; ++i)
      {
        // Keep updating the iterators that are not done.
        Core.BehaviourIterator itr = subIterators[i];
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