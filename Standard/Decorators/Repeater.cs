
using Bonsai.Core;
using Bonsai.Designer;

namespace Bonsai.Standard
{
    [NodeEditorProperties("Decorators/", "RepeatArrow")]
    public sealed class Repeater : Decorator
    {
        public int loopCount = 1;
        public bool infiniteLoop = false;

        private int _loopCounter = 0;

        public override void OnEnter()
        {
            _loopCounter = 0;
        }

        public override Status Run()
        {
            // Infinite loop always returns running and always traverses the child.
            if (infiniteLoop) {
                _iterator.Traverse(_child);
                return Status.Running;
            }

            else {

                // If we have not exceeded the loop count then traverse the child.
                if (_loopCounter < loopCount) {
                    _loopCounter++;
                    _iterator.Traverse(_child);
                    return Status.Running;
                }

                // Finished looping, return what the child returns.
                else {
                    return _iterator.LastStatusReturned;
                }
            }
        }
    }
}