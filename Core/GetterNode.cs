namespace Bonsai.Core
{
    public abstract class GetterNode : BehaviourNode
    {
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

    /// <summary>
    /// Base class for a node that takes some values.
    /// </summary>
    public abstract class GetterNode<T> : GetterNode
    {
        public abstract T Get();
    }
}