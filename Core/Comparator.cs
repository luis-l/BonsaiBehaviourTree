using System;
using UnityEngine;

namespace Bonsai.Core
{
    /// <summary>
    /// The base class for two getter nodes.
    /// </summary>
    public abstract class Comparator : BehaviourNode
    {
        public sealed override int MaxChildCount()
        {
            return 2;
        }

        public abstract void SetChildX(object childrenX);
        public abstract void SetChildY(object childrenY);
    }

    public abstract class Comparator<T> : Comparator
    {
        [SerializeField, HideInInspector] private GetterNode<T> childrenX;
        [SerializeField, HideInInspector] private GetterNode<T> childrenY;

        protected abstract bool Compare(T x, T y);

        public sealed override Status Run()
        {
            var x = childrenX.Get();
            var y = childrenY.Get();
            return Compare(x, y) ? Status.Success : Status.Failure;
        }

        public sealed override void SetChildX(object childX)
        {
            childrenX = childX as GetterNode<T>;
        }

        public sealed override void SetChildY(object childY)
        {
            childrenY = childY as GetterNode<T>;
        }

        public sealed override int ChildCount()
        {
            var count = 0;
            if (childrenX) count++;
            if (childrenY) count++;

            return count;
        }

        public sealed override BehaviourNode GetChildAt(int index)
        {
            return index switch
            {
                0 => childrenX,
                1 => childrenY,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }
    }
}