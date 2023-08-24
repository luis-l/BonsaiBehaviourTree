namespace Bonsai.Core
{
    /// <summary>
    /// Base class for a node that takes some values.
    /// </summary>
    public abstract class GetterNode<T> : BehaviourNode
    {
        public abstract T Get();

        public sealed override Status Run()
        {
            return Status.Success;
        }

        public sealed override BehaviourNode GetChildAt(int index)
        {
            return null;
        }

        public sealed override int ChildCount()
        {
            return 0;
        }

        public sealed override int MaxChildCount()
        {
            return 0;
        }
    }
}