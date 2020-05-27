
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
    /// <summary>
    /// Re-traversing the child until it returns failure.
    /// </summary>
    [NodeEditorProperties("Decorators/", "RepeatCross")]
    public class UntilFailure : Decorator
    {
        public override Status Run()
        {
            Status s = _iterator.LastStatusReturned;

            if (s == Status.Failure) {
                return Status.Success;
            }

            // Retraverse child.
            _iterator.Traverse(_child);

            return Status.Running;
        }
    }
}