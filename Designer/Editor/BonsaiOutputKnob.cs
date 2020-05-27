
using System.Collections.Generic;
using UnityEngine;

using Bonsai.Core;

namespace Bonsai.Designer
{
    public class BonsaiOutputKnob : BonsaiKnob
    {
        private List<BonsaiInputKnob> _inputs = new List<BonsaiInputKnob>();

        public IEnumerable<BonsaiInputKnob> InputConnections
        {
            get { return _inputs; }
        }

        public BonsaiOutputKnob()
        {
            background = BonsaiResources.GetTexture("DarkGray");
        }

        public bool Contains(BonsaiInputKnob input)
        {
            return _inputs.Contains(input);
        }

        public void Add(BonsaiInputKnob input)
        {
            // Avoid connecting it to a root.
            if (input.parentNode.behaviour == input.parentNode.behaviour.Tree.Root) {
                Debug.LogWarning("A root cannot be a child.");
                return;
            }

            // Avoid re-adding.
            if (Contains(input)) {
                Debug.LogWarning("Already added.");
                return;
            }

            // Avoid cycles.
            if (cycleDetected(input)) {
                Debug.LogWarning("Cycle detected.");
                return;
            }

            // If it is already parented, then unparent it.
            if (input.outputConnection != null) {
                input.outputConnection.RemoveInputConnection(input);
            }

            input.outputConnection = this;

            // Disconnect other inputs since we can only have 1.
            if (!parentNode.bCanHaveMultipleChildren) {
                RemoveAllInputs();
            }

            _inputs.Add(input);

            // Notify the parent that there was a new input.
            parentNode.OnNewInputConnection(input);
        }

        private bool cycleDetected(BonsaiInputKnob input)
        {
            var currentNode = this.parentNode;

            while (currentNode != null) {

                // Cycle detected.
                if (input.parentNode == currentNode) {
                    return true;
                }

                // There are no more parents to traverse to.
                if (currentNode.Input == null || currentNode.Input.outputConnection == null) {
                    break;
                }

                // Move up the tree.
                else {
                    currentNode = currentNode.Input.outputConnection.parentNode;
                }
            }

            // No cycle detected.
            return false;
        }

        internal void RemoveInputConnection(BonsaiInputKnob input)
        {
            if (_inputs.Remove(input)) {
                parentNode.OnInputConnectionRemoved(input);
                input.outputConnection = null;
            }
        }

        internal void RemoveAllInputs()
        {
            foreach (var i in _inputs) {
                parentNode.OnInputConnectionRemoved(i);
                i.outputConnection = null;
            }

            _inputs.Clear();
        }

        /// <summary>
        /// Get the input connection at some index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public BonsaiInputKnob GetInput(int index)
        {
            return _inputs[index];
        }

        /// <summary>
        /// Get the number of connected inputs.
        /// </summary>
        /// <returns></returns>
        public int InputCount()
        {
            return _inputs.Count;
        }

        /// <summary>
        /// Syncs the ordering of the inputs with the internal tree structure.
        /// </summary>
        internal void SyncOrdering()
        {
            var composite = parentNode.behaviour as Composite;

            if (!composite) return;

            // This will make sure that the input knob orders are in sync with the child orders.
            _inputs.Sort();

            for (int i = 0; i < _inputs.Count; ++i) {

                BehaviourNode b = _inputs[i].parentNode.behaviour;

                // We can do this without destroying the association between parent and children nodes
                // since all we are doing is modifying the ordering of the child nodes in the children array.
                composite.SetChildAtIndex(b, i);
            }

            // Just make sure to sync the index ordering after swapping children.
            composite.UpdateIndexOrders();
        }

        /// <summary>
        /// Returns the y coordinate of the nearest input knob on the y axis.
        /// </summary>
        /// <returns></returns>
        internal float GetNearestInputY()
        {
            float nearestY = float.MaxValue;
            float nearestDist = float.MaxValue;

            foreach (BonsaiInputKnob input in _inputs) {

                Vector2 toChild = input.bodyRect.position - parentNode.bodyRect.position;

                float yDist = Mathf.Abs(toChild.y);

                if (yDist < nearestDist) {
                    nearestDist = yDist;
                    nearestY = input.bodyRect.position.y;
                }
            }

            return nearestY;
        }

        /// <summary>
        /// Gets the max and min x coordinates between the children and the parent.
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        internal void GetBoundsX(out float minX, out float maxX)
        {
            minX = parentNode.bodyRect.center.x;
            maxX = parentNode.bodyRect.center.x;

            foreach (BonsaiInputKnob input in _inputs) {

                float x = input.parentNode.bodyRect.center.x;

                if (x < minX) {
                    minX = x;
                }

                else if (x > maxX) {
                    maxX = x;
                }
            }
        }
    }
}