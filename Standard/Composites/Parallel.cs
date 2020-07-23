
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

      RunChildBranches();

      // Parallel iterators still running.
      return Status.Running;
    }
  }
}